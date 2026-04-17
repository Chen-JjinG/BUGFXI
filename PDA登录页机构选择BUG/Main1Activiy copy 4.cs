using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Telephony;
using Android.Text.Method;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using K9PDA.Infrastructure.Model;
using K9PDA.Infrastructure.Request;
using K9PDA.Infrastructure.ServiceProxy;
using K9PDA.Infrastructure.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using static Android.Widget.AdapterView;

/* ------------------------------------------------
版本记录      版本日期      编辑人      编辑内容
V1.1          2026-03-26    CJJ       BUG#77170 输入账号且失去Focus时用于回写部门
V1.2          2026-03-28    CJJ       SetTheme调用时机错误
                                      GetListData(Action<>)参数覆盖逻辑反转
                                      WriteBackDept中data[2]越界
                                      CmbAccount_ItemSelected机构框显示编码而非名称
                                      CreateFile异步写入未等待
                                      GetValue中XPath字符串拼接注入风险
V1.3          2026-04-07    ZZH       增加动盘
--------------------------------------------------- */


namespace K9PDA
{
    [Activity(MainLauncher = true, Icon = "@drawable/NeoLogo")]
    public class Main1Activity : BaseFrom, PopupWindow.IOnDismissListener, IOnItemClickListener, View.IOnTouchListener
    {
        View rootView;
        GlobalLayoutListener globalLayoutListener = new GlobalLayoutListener();
        TextView txtAccount;
        EditText txtOrg;
        TextView txtDepart;
        EditText txtUserAccount;
        EditText txtPwd;
        EditText txtTenantid;
        string OrgID;
        string DepartID;
        string strTenantId;
        private SpinnerPopWindowAdapter<string> spinnerPopAccount;
        private SpinnerPopWindowAdapter<string> spinnerPopOrg;
        private SpinnerPopWindowAdapter<string> spinnerPopDepart;
        private bool isListenerAttached = false;

        int mtype = 0;

        private List<AccountInfo> Accounts;
        private List<string> listAccount;
        private AccountInfo accountInfoCurrSelect;
        private List<string> listDepart;
        private List<string> listOrg;

        List<ComboItem> orgList;
        List<ComboItem> deptList;

        ComboItem orgData;
        ComboItem deptData;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            //V1.2 SetTheme 必须在 base.OnCreate 之前调用，否则主题不生效
            SetTheme(Android.Resource.Style.ThemeLightNoTitleBar);

            base.OnCreate(savedInstanceState);

            InitSystem();
            StartActivityForResult(new Intent(this, typeof(SplashScreenActivity)), 20);
            SetContentView(Resource.Layout.Main1);

            rootView = FindViewById(Resource.Id.root_View);
            InitializeViews();
        }

        public override bool DispatchTouchEvent(MotionEvent e)
        {
            if (e.Action == MotionEventActions.Down)
            {
                if (CurrentFocus != null && CurrentFocus is EditText)
                {
                    var focusedView = CurrentFocus as EditText;
                    if (txtOrg != null && focusedView != txtOrg && focusedView != txtUserAccount
                        && focusedView != txtDepart && focusedView != txtPwd && focusedView != txtTenantid)
                    {
                        focusedView.ClearFocus();
                    }
                }
            }
            return base.DispatchTouchEvent(e);
        }

        private void InitializeViews()
        {
            ImageView ac = FindViewById<ImageView>(Resource.Id.actionMenuView1);
            ac.Click += Ac_Click;
            txtPwd = FindViewById<EditText>(Resource.Id.txtPwd);
            txtPwd.InputType = Android.Text.InputTypes.TextVariationPassword;
            txtPwd.TransformationMethod = PasswordTransformationMethod.Instance;
            //txtPwd.TextChanged += (s, e)=>{

            //};
            var _buttom = FindViewById<Button>(Resource.Id.button1);//登录
            _buttom.Click += _buttom_Click;

            //rootView.ViewTreeObserver.AddOnGlobalFocusChangeListener(
            //        new FocusChangeListener((o, n) =>
            //        {
            //            string msg = $"焦点变化，{o.ToString()}变为{n.ToString()}";
            //            Toast.MakeText(this,msg, 0).Show();
            //            //Log.Debug($"TESTDEBUG_{DateTime.Now.ToString()}", msg);
            //        })
            //    );


            this.txtOrg = FindViewById<EditText>(Resource.Id.txtOrg);
            txtOrg.SetOnTouchListener(this);

            txtUserAccount = FindViewById<EditText>(Resource.Id.txtUserAccount);
            txtAccount = FindViewById<TextView>(Resource.Id.txtAccount);
            txtDepart = FindViewById<TextView>(Resource.Id.txtDepart);
            txtTenantid = FindViewById<EditText>(Resource.Id.txtTenantId);

            if (GlobalDataCache.PDALoginProduct == "2")
            {
                txtAccount.Visibility = ViewStates.Gone;
                //txtAccount.Hint = "请输入租户信息";
                txtOrg.Visibility = ViewStates.Gone;
                txtDepart.Visibility = ViewStates.Gone;
                txtTenantid.Visibility = ViewStates.Visible;
            }
            else
            {
                txtTenantid.Visibility = ViewStates.Gone;
                txtAccount.Visibility = ViewStates.Visible;
                txtOrg.Visibility = ViewStates.Visible;
                txtDepart.Visibility = ViewStates.Visible;
            }
            this.txtOrg.KeyPress += TxtOrg_KeyPress;
            this.txtOrg.FocusChange += TxtOrg_FocusChange;

            //txtUserAccount.KeyPress += TxtUserAccount_KeyPress;
            txtUserAccount.FocusChange += TxtUserAccount_FocusChange;//V1.1

            this.txtTenantid.KeyPress += TxtTenantid_KeyPress;
            //this.txtUserAccount.TextChanged += TxtUserAccount_TextChanged;
            this.txtPwd.FocusChange += TxtPwd_FocusChange;
            //this.txtPwd.TextChanged += TxtPwd_TextChanged;
            //txtPwd.ViewTreeObserver.AddOnGlobalLayoutListener(new GlobalLayoutListener());
            //SetTextImage(txtAccount, Resource.Drawable.Account, 24, 24);
            //SetTextImage(txtOrg, Resource.Drawable.corg, 24, 24);
            //SetTextImage(txtDepart, Resource.Drawable.depart, 24, 24);
            //SetTextImage(txtUserAccount, Resource.Drawable.UserAccount, 24, 24);
            //SetTextImage(txtPwd, Resource.Drawable.password, 24, 24);
            globalLayoutListener.rootView = rootView;
        }

        private void AttachLayoutListener()
        {
            if (!isListenerAttached)
            {
                rootView.ViewTreeObserver.AddOnGlobalLayoutListener(globalLayoutListener);
                isListenerAttached = true;
            }
        }

        private void DetachLayoutListener()
        {
            if (isListenerAttached)
            {
                rootView.ViewTreeObserver.RemoveOnGlobalLayoutListener(globalLayoutListener);
                isListenerAttached = false;
            }
        }

        bool View.IOnTouchListener.OnTouch(View v, MotionEvent e)
        {
            switch (e.Action)
            {
                case MotionEventActions.Down:
                    var context = Android.App.Application.Context;
                    var right = context.Resources.GetDrawable(Resource.Drawable.expand, context.Theme);
                    if (right != null && e.RawX + 20 >= (v as EditText).Right - right.Bounds.Width())
                    {
                        (v as EditText).ShowSoftInputOnFocus = false;
                        InputMethodManager inputMethodManager = (InputMethodManager)GetSystemService(Context.InputMethodService);
                        inputMethodManager.HideSoftInputFromWindow(this.CurrentFocus.WindowToken, HideSoftInputFlags.NotAlways);
                        txtUserAccount.ClearFocus();
                        txtDepart.ClearFocus();
                        txtPwd.ClearFocus();
                        ShowChooseOrg();
                    }
                    break;
                default:
                    break;
            }
            return base.OnTouchEvent(e);
        }

        private void TxtPwd_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void TxtUserAccount_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.txtUserAccount.Text) && !string.IsNullOrWhiteSpace(this.txtOrg.Text))
            {
                //ShowChooseOrg();
                this.txtUserAccount.Focusable = true;
                this.txtUserAccount.FocusableInTouchMode = true;
                this.txtUserAccount.RequestFocus();
                this.txtUserAccount.FindFocus();
            }
        }

        private void TxtPwd_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            globalLayoutListener.currentEditText = sender as EditText;
            if (!e.HasFocus)
            {
                //if (string.IsNullOrWhiteSpace(this.txtPwd.Text) && !string.IsNullOrWhiteSpace(this.txtOrg.Text))
                //{
                //    //ShowChooseOrg();
                //    this.txtPwd.Focusable = true;
                //    this.txtPwd.FocusableInTouchMode = true;
                //    this.txtPwd.RequestFocus();
                //    this.txtPwd.FindFocus();
                //}
                //rootView.ViewTreeObserver.RemoveGlobalOnLayoutListener(globalLayoutListener);
                DetachLayoutListener();
            }
            else
            {
                //rootView.ViewTreeObserver.AddOnGlobalLayoutListener(globalLayoutListener);
                AttachLayoutListener();
            }
        }

        //V1.1
        private void TxtUserAccount_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            globalLayoutListener.currentEditText = sender as EditText;
            if (!e.HasFocus)
            {
                if (!string.IsNullOrWhiteSpace(txtUserAccount.Text) && orgData != null)
                {
                    WriteBackDept();
                }
                DetachLayoutListener();
            }
            else
            {
                AttachLayoutListener();
            }
        }
        private void TxtTenantid_KeyPress(object sender, View.KeyEventArgs e)
        {
            e.Handled = false;
            if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Up)
            {
                if (!string.IsNullOrWhiteSpace(txtTenantid.Text))
                {
                    e.Handled = true;
                    strTenantId = txtTenantid.Text;
                    this.txtUserAccount.Focusable = true;
                    this.txtUserAccount.RequestFocus();
                }
            }
            else if (e.KeyCode != Keycode.Enter)
            {
                this.txtTenantid.Tag = null;
                strTenantId = "";
            }
        }

        /// <summary>
        /// 切换账套信息
        /// </summary>
        private void CmbAccount_ItemSelected()
        {
            orgData = null;
            deptData = null;
            this.txtOrg.Tag = null;
            this.txtOrg.Text = "";
            OrgID = "";

            accountInfoCurrSelect = new AccountInfo();
            accountInfoCurrSelect = Accounts.Where(c => c.Name == txtAccount.Text).FirstOrDefault();
            if (accountInfoCurrSelect == null)
            {
                return;
            }
            //txtUserAccount.Text = AppSettings.GetValue("Setting.config", "LoginUser");// "admin"; 
            Proxy proxy = new Infrastructure.ServiceProxy.Proxy();
            GlobalProxySetting.AccountName = accountInfoCurrSelect.Name;

            proxy.ApplySetting(GlobalProxySetting);
            GetListData((data) =>
            {
                orgList = data;
                var tmpName = AppSettings.GetValue("Setting.config", "OrgID");
                var org = orgList.FirstOrDefault(o => o.Data.ToString() == tmpName);
                if (org != null)
                {
                    orgData = org;
                    this.txtOrg.Tag = new ComboxItemYQ() { Item = org };
                    OrgID = (this.txtOrg.Tag as ComboxItemYQ).Item.Data.ToString();//记录当前选择的机构编码
                    //V1.2 原代码将 tmpName（机构编码）赋给显示框，用户看到的是编码而非名称
                    //应使用 org.Text（机构名称）回写到 txtOrg
                    this.txtOrg.Text = org.Text;
                    this.txtUserAccount.RequestFocus();
                    deptList = GetListData(orgData.Data.ToString());
                    DepartAdapter();
                }
                else
                {
                    //this.txtOrg.RequestFocus();
                    ShowChooseOrg();
                }
            });
        }
        private void TxtUserAccount_KeyPress(object sender, View.KeyEventArgs e)
        {
            e.Handled = false;
            if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Up)
            {
                WriteBackDept();
                if (orgData != null && !string.IsNullOrEmpty(OrgID))
                {
                    this.txtPwd.Focusable = true;
                    this.txtPwd.FocusableInTouchMode = true;
                    this.txtPwd.RequestFocus();
                }
                e.Handled = true;
            }
        }

        private void WriteBackDept()
        {
            if (GlobalDataCache.PDALoginProduct == "2")
            {
                if (string.IsNullOrEmpty(txtUserAccount.Text))
                {
                    Toast.MakeText(this, "请输入账号信息！", 0).Show();
                    return;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(txtUserAccount.Text))
                {
                    return;
                }
                if (orgData == null || string.IsNullOrEmpty(OrgID))
                {
                    Toast.MakeText(this, "请先选择机构！", 0).Show();
                    return;
                }

                var data = GetListData(OrgID, "-");
                if (data != null && data.Count > 0)
                {
                    if (data[0].Data.GetType() == typeof(Newtonsoft.Json.Linq.JArray))
                    {
                        var returnedName = data[0].Text;
                        if (!string.IsNullOrEmpty(returnedName) && returnedName != txtUserAccount.Text.Trim())
                        {
                            this.txtUserAccount.Text = returnedName;
                        }
                    }
                    if (data.Count > 2 && data[2].Data.GetType() == typeof(Newtonsoft.Json.Linq.JArray))
                    {
                        var returnedDeptId = data[2].Text;
                        if (!string.IsNullOrEmpty(returnedDeptId) && returnedDeptId != "-")
                        {
                            DepartID = returnedDeptId;
                            if (deptList != null && deptList.Count > 0)
                            {
                                var deptComboItem = deptList.Where(w => w.Data.ToString() == returnedDeptId).FirstOrDefault();
                                if (deptComboItem != null)
                                {
                                    this.txtDepart.Text = deptComboItem.Text;
                                }
                            }
                            else
                            {
                                DepartID = "";
                            }
                        }
                    }
                }
            }
        }

        private void TxtOrg_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            if (!e.HasFocus)
            {
                if (spinnerPopOrg != null && spinnerPopOrg.IsShowing)
                {
                    return;
                }
                if (string.IsNullOrWhiteSpace(this.txtOrg.Text) && !string.IsNullOrWhiteSpace(txtAccount.Text))
                {
                    ShowChooseOrg();
                }
            }
        }

        private void TxtOrg_KeyPress(object sender, View.KeyEventArgs e)
        {
            e.Handled = false;
            if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Up)
            {
                //if (string.IsNullOrWhiteSpace(this.txtOrg.Text) && !string.IsNullOrWhiteSpace(txtAccount.Text))
                //{
                e.Handled = true;
                ShowChooseOrg();
                //}
            }
            else if (e.KeyCode != Keycode.Enter)
            {
                this.txtOrg.Tag = null;
                orgData = null;
                OrgID = "";
            }
        }

        private void ShowChooseOrg()
        {
            deptData = null;
            DepartID = "";
            this.txtDepart.Text = "";
            if (GlobalDataCache.PDALoginProduct == "2")
            {
                if (string.IsNullOrEmpty(txtOrg.Text))
                {
                    Toast.MakeText(this, "请输入租户信息！", 0).Show();
                    return;
                }
                else
                {
                    this.txtUserAccount.Focusable = true;
                    this.txtUserAccount.FocusableInTouchMode = true;
                    this.txtUserAccount.RequestFocus();
                }
                OrgID = txtOrg.Text;
                return;
            }

            if (orgList == null)
            {
                Toast.MakeText(this, "请选择账套！", 0).Show();
                return;
            }
            var filterList = orgList.Where(c => c.ZJM.ToUpper().Contains(this.txtOrg.Text.ToUpper()) || c.Text.Contains(this.txtOrg.Text)).ToArray();
            var dialogList = filterList.Select(c => c.Data + "(" + c.Text + ")").ToArray();
            if (dialogList.Length > 1)
            {
                #region
                //new AlertDialog.Builder(this).SetTitle("请选择").SetItems(dialogList,
                //         (senders, ea) =>
                //         {
                //             if (ea.Which >= 0)
                //             {
                //                 var item = filterList[ea.Which];
                //                 orgData = item;
                //                 this.txtOrg.Tag = new ComboxItemYQ { Item = item };
                //                 this.txtOrg.Text = item.Text;
                //                 OrgID = (this.txtOrg.Tag as ComboxItemYQ).Item.Data.ToString();//记录当前选择的机构编码
                //                 this.txtUserAccount.RequestFocus();
                //                 GetListData((data) =>
                //                 {
                //                     deptList = GetListData(orgData.Data.ToString());
                //                     //cmbDept.Adapter = new K9PDA.Adapter.SpinnerAdapter(this, Android.Resource.Layout.SimpleSpinnerItem, deptList);
                //                     //this.cmbDept.Enabled = deptList.Count > 0;
                //                     DepartAdapter();
                //                 });
                //             }
                //             else
                //             {
                //                 orgData = null;
                //                 this.txtOrg.Tag = null;
                //                 this.txtOrg.Text = "";
                //                 OrgID = "";
                //             }
                //         }).Show();
                #endregion

                listOrg = new List<string>();
                foreach (var orgitem in filterList)
                {
                    listOrg.Add(orgitem.Text);
                }
                //如果不存在，默认显示第一条
                //if (listOrg.Count > 0)
                //{
                //    if (!listOrg.Where(w => w == txtOrg.Text).Any())
                //    {
                //        txtOrg.Text = listOrg[0].ToString();
                //    }
                //    orgData = filterList.Where(c => c.Text == txtOrg.Text).FirstOrDefault();
                //    OrgID = orgData == null ? "" : orgData.Data.ToString();//记录当前选择部门编码
                //}

                spinnerPopOrg = new SpinnerPopWindowAdapter<string>(this, listOrg, this);
                spinnerPopOrg.SetOnDismissListener(this);
                mtype = 2;
                spinnerPopOrg.Width = txtOrg.Width;
                spinnerPopOrg.ShowAsDropDown(txtOrg);
            }
            else if (dialogList.Length == 1)
            {
                var item = filterList[0];
                orgData = item;
                this.txtOrg.Tag = new ComboxItemYQ { Item = item };
                this.txtOrg.Text = item.Text;
                OrgID = (this.txtOrg.Tag as ComboxItemYQ).Item.Data.ToString();//记录当前选择的机构编码
                this.txtUserAccount.RequestFocus();
                GetListData((data) =>
                {
                    deptList = GetListData(orgData.Data.ToString());
                    //cmbDept.Adapter = new K9PDA.Adapter.SpinnerAdapter(this, Android.Resource.Layout.SimpleSpinnerItem, deptList);
                    //this.cmbDept.Enabled = deptList.Count > 0;
                    DepartAdapter();
                });
            }
            else
            {
                orgData = null;
                this.txtOrg.Tag = null;
                OrgID = "";
                Toast.MakeText(this, "找不到此机构信息！", 0).Show();
            }
        }

        class ComboxItemYQ : Java.Lang.Object
        {
            public ComboItem Item { get; set; }
        }

        private void GetListData(Action<List<ComboItem>> dataCallBack, string orgID = "", string deptID = "")
        {
            var accountName = GlobalProxySetting.AccountName;
            //V1.2 原代码在参数非空时将其覆盖为成员变量的值，逻辑完全反向
            //应与同步重载保持一致：参数为空时才用成员变量补充，且需 null 保护
            if (string.IsNullOrWhiteSpace(orgID) && orgData != null)
            {
                orgID = orgData.Data.ToString();
            }
            if (string.IsNullOrWhiteSpace(deptID) && deptData != null)
            {
                deptID = deptData.Data.ToString();
            }
            this.Proxy.Execute(new GetUserAccountNameRequest()
            {
                AccountName = accountName,
                OrgID = orgID,
                DeptID = deptID,
                UserAccountCode = txtUserAccount.Text == null ? "" : txtUserAccount.Text.ToString().Trim(),
                GetUnVisibleOrg = false
            }, (response) =>
            {
                if (response.IsError)
                {
                    dataCallBack(new List<ComboItem>());
                    return;
                }
                dataCallBack(response.Result);
            }, this);
        }

        private List<ComboItem> GetListData(string orgID = "", string deptID = "")
        {
            var accountName = GlobalProxySetting.AccountName;
            if (string.IsNullOrWhiteSpace(orgID) && orgData != null)
            {
                orgID = orgData.Data.ToString();
            }
            if (string.IsNullOrWhiteSpace(deptID) && deptData != null)
            {
                deptID = deptData.Data.ToString();
            }
            var response = this.Proxy.Execute(new GetUserAccountNameRequest()
            {
                AccountName = accountName,
                OrgID = orgID,
                DeptID = deptID,
                UserAccountCode = txtUserAccount.Text == null ? "" : txtUserAccount.Text.ToString().Trim(),
                GetUnVisibleOrg = false
            });
            if (response.IsError)
            {
                return new List<ComboItem>();
            }
            return response.Result;
        }
        private void SetTextImage(EditText textView, int idrawable, int iwidth, int iheight)
        {
            //SetTextImage
            Drawable drawable = Resources.GetDrawable(idrawable);
            drawable.SetBounds(0, 0, iwidth, iheight);
            Drawable spinDraw = null;
            if (textView.Id == Resource.Id.txtAccount || textView.Id == Resource.Id.txtDepart)
            {
                spinDraw = Resources.GetDrawable(Resource.Drawable.spinner);
                spinDraw.SetBounds(0, 0, 32, 32);
            }
            textView.SetCompoundDrawables(drawable, null, spinDraw, null);
        }

        private void SetTextImage(TextView textView, int idrawable, int iwidth, int iheight)
        {
            //SetTextImage
            Drawable drawable = Resources.GetDrawable(idrawable);
            drawable.SetBounds(0, 0, iwidth, iheight);
            Drawable spinDraw = null;
            if (textView.Id == Resource.Id.txtAccount || textView.Id == Resource.Id.txtDepart)
            {
                spinDraw = Resources.GetDrawable(Resource.Drawable.spinner);
                spinDraw.SetBounds(0, 0, 32, 32);
            }
            textView.SetCompoundDrawables(drawable, null, spinDraw, null);
        }
        /// <summary>
        /// popupWindow 显示的ListView的item点击事件
        /// </summary>
        public void OnItemClick(AdapterView parent, View view, int position, long id)
        {
            switch (mtype)
            {
                case 0:
                    spinnerPopAccount.Dismiss();
                    if (listAccount != null && listAccount.Count > 0)
                    {
                        txtAccount.Text = listAccount[position].ToString();
                        CmbAccount_ItemSelected();
                    }
                    break;
                case 1:
                    spinnerPopDepart.Dismiss();
                    if (listDepart != null && listDepart.Count > 0)
                    {
                        txtDepart.Text = listDepart[position].ToString();
                        deptData = deptList.Where(c => c.Text == txtDepart.Text).FirstOrDefault();
                        DepartID = deptData == null ? "" : deptData.Data.ToString();//记录当前选择部门编码
                    }
                    break;
                case 2:
                    spinnerPopOrg.Dismiss();
                    if (listOrg != null && listOrg.Count > 0 && position >= 0 && position < listOrg.Count)
                    {
                        txtOrg.Text = listOrg[position].ToString();
                        orgData = orgList[position];
                        OrgID = orgData == null ? "" : orgData.Data.ToString();
                        DepartID = "";
                        this.txtDepart.Text = "";
                        deptData = null;
                        if (orgData != null)
                        {
                            GetListData((data) =>
                            {
                                deptList = GetListData(orgData.Data.ToString());
                                DepartAdapter();
                            });
                        }
                    }
                    break;
                default:
                    break;
            }

        }
        /// <summary>
        /// popupWindow取消
        /// </summary>
        public void OnDismiss()
        {
            //SetTextImage(Resource.Drawable.spinner);
        }

        private void DepartAdapter()
        {
            //txtDepart.Text = "";
            listDepart = new List<string>();
            foreach (var depitem in deptList)
            {
                listDepart.Add(depitem.Text);
            }
            //如果不存在，默认显示第一条
            if (listDepart.Count > 0)
            {
                if (!listDepart.Where(w => w == txtDepart.Text).Any())
                {
                    txtDepart.Text = listDepart[0].ToString();
                }
                deptData = deptList.Where(c => c.Text == txtDepart.Text).FirstOrDefault();
                DepartID = deptData == null ? "" : deptData.Data.ToString();//记录当前选择部门编码
            }

            spinnerPopDepart = new SpinnerPopWindowAdapter<string>(this, listDepart, this);
            spinnerPopDepart.SetOnDismissListener(this);
            txtDepart.Click += (s, e) =>
            {
                mtype = 1;
                spinnerPopDepart.Width = txtAccount.Width;
                spinnerPopDepart.ShowAsDropDown(txtDepart);
                //SetTextImage(Resource.Drawable.icon_up);
            };
        }

        private void _buttom_Click(object sender, EventArgs e)
        {
            if (GlobalDataCache.PDALoginProduct == "2")
            {
                if (string.IsNullOrWhiteSpace(txtTenantid.Text))
                {
                    Toast.MakeText(this, "请先填写租户信息！", 0).Show();
                    return;
                }
                else
                {
                    txtAccount.Text = txtTenantid.Text;//租户id赋值到账套中
                    GlobalProxySetting.AccountName = txtTenantid.Text;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(OrgID))
                {
                    Toast.MakeText(this, "请选择机构后再登录！", 0).Show();
                    return;
                }
            }
            if (txtUserAccount.Text == "")
            {
                Toast.MakeText(this, "请填写账号！", 0).Show();
                return;
            }

            //OrgID = (this.txtOrg.Tag as ComboxItemYQ).Item.Data.ToString();
            //DepartID = deptData == null ? "" : deptData.Data.ToString();
            Proxy.Execute(new PDALoginRequest()
            {
                AccountName = GlobalProxySetting.AccountName,
                Name = txtUserAccount.Text.Trim(),
                Pwd = txtPwd.Text,
                ClientName = System.Net.Dns.GetHostName(),
                MachineCode = SystemInfo.GetMachineCode(),
                OrgID = OrgID,
                DeptID = DepartID,
            }, (response) =>
            {
                try
                {
                    if (response.IsError)
                    {
                        Toast.MakeText(this, response.ErrorMessage, 0).Show();
                        return;
                    }
                    if (!response.Result.State.Success)
                    {
                        Toast.MakeText(this, response.Result.State.Message, 0).Show();
                        return;
                    }
                    GlobalProxySetting.GetLoginState = () => { return response.Result.Info; };
                    GlobalProxySetting.ConfigList = response.Result.ConfigList;

                    //if(GlobalProxySetting.ConfigList.Any(p=>p.cparmname == "UseZsmNewProcess"))
                    //{
                    //    var realTime v a lue = ConvertHelper.ToInt(GlobalDataCache.DBAccess.ExecuteScalar("SELECT TOP 1 cparmvalue FROM dbo.sy_config WHERE cparmname = 'UseZsmNewProcess'")).ToString();
                    //    GlobalProxySetting.ConfigList.FirstOrDefault(p => p.cparmname == "UseZsmNewProcess").cparmvalue = realTime v a lue;
                    //}

                    GlobalDataCache.LoginUserInfo = response.Result.Info;

                    GlobalProxySetting.PDAUserSpecialPlanInfo = response.Result.PDAUserSpecialPlanInfo;
                    GlobalProxySetting.UserID = txtUserAccount.Text.Trim();
                    GlobalProxySetting.OrgID = OrgID;// (this.txtOrg.Tag as ComboxItemYQ).Item.Data.ToString();
                    if (GlobalDataCache.PDALoginProduct == "2")
                    {
                        GlobalProxySetting.AccountName = txtAccount.Text.ToString();
                        GlobalProxySetting.OrgID = response.Result.Info.OrgID;
                        if (response.Result != null && response.Result.Info != null)
                        {
                            txtOrg.Text = response.Result.Info.OrgName;
                            DepartID = response.Result.Info.DeptID;
                            txtDepart.Text = response.Result.Info.DepName;
                        }
                    }
                    /*药监码采集授权校验*/
                    if (GlobalDataCache.AppProductName == "K9PDA.K9TRACE")
                    {
                        var request = new BusinessRequest() { BusinessKey = "PDATraceCodeProcess" };
                        request.Parameters["opType"] = 100;
                        var responseLimit = this.Proxy.Execute(request);

#if !DEBUG
                        if (responseLimit.IsError)
                        {
                            Toast.MakeText(this, "应用未授权！" + responseLimit.ErrorMessage, 0).Show();
                            return;
                        }
#endif
                        GlobalDataCache.PDATRACELimitON = true;
                    }

                    if (response.Result.ConfigList == null || response.Result.ConfigList.Count == 0)
                    {
                        #region K8PDA接口不返回ConfigList
                        GlobalDataCache.QtyFormat = "F2";
                        GlobalDataCache.PriceFormat = "F2";
                        GlobalDataCache.ValueFormat = "F2";
                        GlobalDataCache.EQUALSTOCKQTY = true;
                        GlobalDataCache.SHOWSTOCKQTY = true;
                        GlobalDataCache.EnableCjzyl1 = false;
                        GlobalDataCache.EnableCjzyl2 = false;
                        GlobalDataCache.EnableWMS = false;
                        GlobalDataCache.PDAPDLimitON = true;
                        GlobalDataCache.PDAYJBGLimitON = false;
                        GlobalDataCache.PDASHLimitON = false;
                        GlobalDataCache.PDAKCLimitON = false;
                        GlobalDataCache.PDAJHLimitON = false;
                        GlobalDataCache.PDAHWLimitON = false;
                        GlobalDataCache.PDAPDSPLimitON = false;
                        GlobalDataCache.PDAMDSHLimitON = false;
                        GlobalDataCache.PDACCHISLimitON = false;
                        GlobalDataCache.PDASHYSLimitON = false;
                        GlobalDataCache.PDALoginProduct = "3";
                        GlobalDataCache.PDAK9NEW3PLlimitON = false;
                        GlobalDataCache.PDACGRKLimitON = false;//入库权限
                        GlobalDataCache.PDAQTCKLimitON = false;//其他出库权限
                        GlobalDataCache.PDAKCYKLimitON = false;//库存移库权限
                        GlobalDataCache.PDAYKXSDDLimitON = false;//移库销售订单权限
                        GlobalDataCache.PDAXSYKLimitON = false;//销售移库权限
                        GlobalDataCache.PDADPLimitON = false;//动盘权限
                        #endregion
                    }
                    else
                    {
                        if (response.Result.ConfigList.Where(c => c.cparmname == "QTYDECLEN").Any())
                            GlobalDataCache.QtyFormat = "F" + response.Result.ConfigList.First(c => c.cparmname == "QTYDECLEN").cparmvalue;
                        else
                            GlobalDataCache.QtyFormat = "F2";
                        if (response.Result.ConfigList.Where(c => c.cparmname == "PRICEDECLEN").Any())
                            GlobalDataCache.PriceFormat = "F" + response.Result.ConfigList.First(c => c.cparmname == "PRICEDECLEN").cparmvalue;
                        else
                            GlobalDataCache.PriceFormat = "F2";
                        if (response.Result.ConfigList.Where(c => c.cparmname == "VALUEDECLEN").Any())
                            GlobalDataCache.ValueFormat = "F" + response.Result.ConfigList.First(c => c.cparmname == "VALUEDECLEN").cparmvalue;
                        else
                            GlobalDataCache.ValueFormat = "F2";
                        if (response.Result.ConfigList.Where(c => c.cparmname == "EQUALSTOCKQTY").Any())
                            GlobalDataCache.EQUALSTOCKQTY = response.Result.ConfigList.First(c => c.cparmname == "EQUALSTOCKQTY").cparmvalue == "1";
                        else
                            GlobalDataCache.EQUALSTOCKQTY = true;
                        if (response.Result.ConfigList.Where(c => c.cparmname == "SHOWSTOCKQTY").Any())
                            GlobalDataCache.SHOWSTOCKQTY = response.Result.ConfigList.First(c => c.cparmname == "SHOWSTOCKQTY").cparmvalue == "1";
                        else
                            GlobalDataCache.SHOWSTOCKQTY = true;

                        if (response.Result.ConfigList.Where(c => c.cparmname == "EnableCjzyl1").Any())
                            GlobalDataCache.EnableCjzyl1 = response.Result.ConfigList.First(c => c.cparmname == "EnableCjzyl1").cparmvalue == "1";
                        else
                            GlobalDataCache.EnableCjzyl1 = false;
                        if (response.Result.ConfigList.Where(c => c.cparmname == "EnableCjzyl2").Any())
                            GlobalDataCache.EnableCjzyl2 = response.Result.ConfigList.First(c => c.cparmname == "EnableCjzyl2").cparmvalue == "1";
                        else
                            GlobalDataCache.EnableCjzyl2 = false;

                        if (response.Result.ConfigList.Where(c => c.cparmname == "WMS_ENABLED").Any())
                            GlobalDataCache.EnableWMS = response.Result.ConfigList.First(c => c.cparmname == "WMS_ENABLED").cparmvalue == "1";
                        else
                            GlobalDataCache.EnableWMS = false;
                        if (response.Result.ConfigList.Where(c => c.cparmname == "TRADE").Any())
                            GlobalDataCache.P_productTRADE = response.Result.ConfigList.First(c => c.cparmname == "TRADE").cparmvalue;
                        if (response.Result.ConfigList.Where(c => c.cparmname == "SOFTUSERTYPE").Any())
                        {
                            GlobalDataCache.PDACCHISLimitON = response.Result.ConfigList.First(c => c.cparmname == "SOFTUSERTYPE").cparmvalue.ToUpper() == "K900191";
                            GlobalDataCache.p_SOFTUSERTYPE = response.Result.ConfigList.First(c => c.cparmname == "SOFTUSERTYPE").cparmvalue;
                        }
                        if (response.Result.ConfigList.Where(c => c.cparmname == "Product").Any())
                            GlobalDataCache.PDAK9NEW3PLlimitON = response.Result.ConfigList.First(c => c.cparmname == "Product").cparmvalue.ToUpper() == "K9_NEW_3PL";
                        else
                            GlobalDataCache.PDAK9NEW3PLlimitON = false;

                        GlobalDataCache.PDAPDLimitON = GetPDAModuleLimit(response.Result.ModuleList, -13);// response.Result.ModuleList.First(c => c.iorder == -13).accesspermission;
                        GlobalDataCache.PDASHLimitON = GetPDAModuleLimit(response.Result.ModuleList, -14);//response.Result.ModuleList.First(c => c.iorder == -14).accesspermission;
                        GlobalDataCache.PDASHYSLimitON = GetPDAModuleLimit(response.Result.ModuleList, -15);//验收权限
                        GlobalDataCache.PDAKCLimitON = GetPDAModuleLimit(response.Result.ModuleList, -16);//response.Result.ModuleList.First(c => c.iorder == -15).accesspermission;
                        GlobalDataCache.PDAJHLimitON = GetPDAModuleLimit(response.Result.ModuleList, -17);//response.Result.ModuleList.First(c => c.iorder == -16).accesspermission;
                        GlobalDataCache.PDAHWLimitON = GetPDAModuleLimit(response.Result.ModuleList, -18);//response.Result.ModuleList.First(c => c.iorder == -17).accesspermission;
                        GlobalDataCache.PDAPDSPLimitON = GetPDAModuleLimit(response.Result.ModuleList, -19);//response.Result.ModuleList.First(c => c.iorder == -18).accesspermission;
                        GlobalDataCache.PDAMDSHLimitON = GetPDAModuleLimit(response.Result.ModuleList, -20);//response.Result.ModuleList.First(c => c.iorder == -19).accesspermission;
                        GlobalDataCache.PDAYJBGLimitON = GetPDAModuleLimit(response.Result.ModuleList, -21);//response.Result.ModuleList.First(c => c.iorder == -20).accesspermission;
                        GlobalDataCache.PDACGRKLimitON = GetPDAModuleLimit(response.Result.ModuleList, -22);//入库权限
                        GlobalDataCache.PDAQTCKLimitON = GetPDAModuleLimit(response.Result.ModuleList, -23);//其他出库权限
                        GlobalDataCache.PDAKCYKLimitON = GetPDAModuleLimit(response.Result.ModuleList, -24);//库存移库权限
                        GlobalDataCache.PDAYKXSDDLimitON = GetPDAModuleLimit(response.Result.ModuleList, -25);//移库销售订单权限
                        GlobalDataCache.PDAXSYKLimitON = GetPDAModuleLimit(response.Result.ModuleList, -26);//销售移库权限
                        GlobalDataCache.PDASJDLimitON = GetPDAModuleLimit(response.Result.ModuleList, -27);//上架
                        GlobalDataCache.PDAFJDLimitON = GetPDAModuleLimit(response.Result.ModuleList, -28);//移位
                        GlobalDataCache.PDAYWLimitON = GetPDAModuleLimit(response.Result.ModuleList, -29);//下架
                        GlobalDataCache.PDACKFHLimitON = GetPDAModuleLimit(response.Result.ModuleList, -30);//出库复核

                        #region 货位盘点原先没有做权限管理，这里同步补上 V1.1

                        GlobalDataCache.PDAHWPDLimitON = false;
                        if (response.Result.ConfigList.Where(c => c.cparmname == "MUILTHWMANAGE").Any())
                            GlobalDataCache.PDAHWPDLimitON = (response.Result.ConfigList.First(c => c.cparmname == "MUILTHWMANAGE").cparmvalue == "1"
                            && GetPDAModuleLimit(response.Result.ModuleList, -31)
                            );

                        #endregion

                        #region PDA的批量采码的权限管理 V1.1

                        GlobalDataCache.PDABatchScanCodeON = false;
                        GlobalDataCache.PDABatchMatchCodeON = false;
                        //启用追溯码并且启用追溯码新流程时再判断权限
                        if ((BaseFrom.GlobalProxySetting.ConfigList.Any(a => (a.cparmname == "ENABLEZSM") && (a.cparmvalue == "1")) && BaseFrom.GlobalProxySetting.ConfigList.Any(a => a.cparmname == "UseZsmNewProcess" && a.cparmvalue == "1")))
                        {
                            GlobalDataCache.PDABatchScanCodeON = GetPDAModuleLimit(response.Result.ModuleList, -32);//批量采码模块
                            GlobalDataCache.PDABatchMatchCodeON = GetPDAModuleLimit(response.Result.ModuleList, -33);//批量采码模块
                        }

                        #endregion

                        GlobalDataCache.PDADPLimitON = GetPDAModuleLimit(response.Result.ModuleList, -34);//动盘V1.3
                        //var cparm = response.Result.ConfigList.First(c => c.cparmname == "SOFTUSERTYPE");//客户定制二次开发代码 2023-04-12 暂时没用到，注释

                    }
                    AppSettings.SetValue("Setting.config", "LoginUser", txtUserAccount.Text.Trim());
                    AppSettings.SetValue("Setting.config", "LoginPsw", txtPwd.Text.Trim());
                    AppSettings.SetValue("Setting.config", "OrgID", OrgID);
                    AppSettings.SetValue("Setting.config", "OrgName", txtOrg.Text.Trim());
                    AppSettings.SetValue("Setting.config", "DeptID", DepartID);
                    AppSettings.SetValue("Setting.config", "DeptName", txtDepart.Text);
                    AppSettings.SetValue("Setting.config", "AccountName", GlobalProxySetting.AccountName);

                    Accounts = GlobalDataCache.GetData<List<AccountInfo>>("Accounts");

                    GlobalDataCache.ClearData();
                    GlobalDataCache.SetData("Accounts", Accounts);

                    //启用追溯码
                    if (GlobalProxySetting.ConfigList.Any(a => (a.cparmname == "ENABLEZSM") && (a.cparmvalue == "1"))
                    && GlobalProxySetting.ConfigList.Any(a => a.cparmname == "UseZsmNewProcess" && a.cparmvalue == "1"))
                    {
                        var DrugCodeMatchInfoGetReqeust = new BusinessRequest() { BusinessKey = "PDADrugCodeInfoInitProcess" };
                        var DrugCodeMatchInfoGetRes = this.Proxy.Execute(DrugCodeMatchInfoGetReqeust);
                        GlobalDataCache.DrugCodeMatchInfo = (DrugCodeMatchInfoGetRes.Result["DrugMatchInfo"] as Newtonsoft.Json.Linq.JObject).ToObject<Dictionary<string, HashSet<PDADrugCodeMatchInfo>>>();// Dictionary<string, HashSet<PDADrugCodeMatchInfo>>
                    }

                    Toast.MakeText(this, "登录成功！", 0).Show();

                    GC.Collect();

                    Intent intent = new Intent(this, typeof(Home));
                    StartActivity(intent);

                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, "确认登录出现异常：" + ex.ToString(), 0).Show();
                    SysLog.WriteLog("确认登录出现异常：" + ex.ToString(), true);
                }

            }, this);

        }

        protected override void OnActivityResult(int requestCode, Android.App.Result resultCode, Intent data)
        {
            if (resultCode == Android.App.Result.Ok)
            {
                Accounts = GlobalDataCache.GetData<List<AccountInfo>>("Accounts");
                /*读取上次登录信息*/
                txtAccount.Text = AppSettings.GetValue("Setting.config", "AccountName");
                txtOrg.Text = AppSettings.GetValue("Setting.config", "OrgName");
                txtDepart.Text = AppSettings.GetValue("Setting.config", "DeptName");
                txtUserAccount.Text = AppSettings.GetValue("Setting.config", "LoginUser");
                txtPwd.Text = AppSettings.GetValue("Setting.config", "LoginPsw");
                OrgID = AppSettings.GetValue("Setting.config", "OrgID");
                DepartID = AppSettings.GetValue("Setting.config", "DeptID");
                if (GlobalDataCache.PDALoginProduct == "2")//显示控制修改
                {
                    txtAccount.Visibility = ViewStates.Gone;
                    txtOrg.Visibility = ViewStates.Gone;
                    txtDepart.Visibility = ViewStates.Gone;
                    txtTenantid.Visibility = ViewStates.Visible;
                }
                else
                {
                    txtTenantid.Visibility = ViewStates.Gone;
                    txtAccount.Visibility = ViewStates.Visible;
                    txtOrg.Visibility = ViewStates.Visible;
                    txtDepart.Visibility = ViewStates.Visible;
                }
                if (GlobalDataCache.PDALoginProduct == "2")
                {
                    txtTenantid.Text = txtAccount.Text;
                }
                else
                {
                    if (Accounts != null)
                    {
                        //cmbAccount.Adapter = new K9PDA.Adapter.SpinnerAdapter(this, Android.Resource.Layout.SimpleSpinnerItem, Accounts);
                        listAccount = new List<string>();
                        foreach (var item in Accounts)
                        {
                            listAccount.Add(item.Name);
                        }
                        spinnerPopAccount = new SpinnerPopWindowAdapter<string>(this, listAccount, this);
                        spinnerPopAccount.SetOnDismissListener(this);
                        txtAccount.Click += (s, e) =>
                        {
                            mtype = 0;
                            spinnerPopAccount.Width = txtAccount.Width;
                            spinnerPopAccount.ShowAsDropDown(txtAccount);
                            //SetTextImage(Resource.Drawable.icon_up);
                        };
                    }


                    CmbAccount_ItemSelected();
                }
            }
            else
            {
                if (requestCode == 20)
                {
                    Finish();
                }
            }
        }

        private void Ac_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(this, typeof(Setting_New));
            StartActivityForResult(intent, 10);
        }

        private bool CreateFile()
        {
            var filePath = AppSettings.AppConfig(ConfigFileName);

            FileStream fileoOS = new FileStream(filePath, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write);
            string str =
string.Format(@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <appSettings>
    <add key=""ServerIP"" value=""{0}""/>
    <add key=""ServerPort"" value=""{1}""/>
    <add key=""LocalCache"" value=""false""/>
    <add key=""AccountName"" value=""{2}""/>
    <add key=""OrgID"" value=""""/>
    <add key=""OrgName"" value=""""/>
    <add key=""DeptID"" value=""""/>
    <add key=""DeptName"" value=""""/>
    <add key=""Token"" value=""{3}""/>
    <add key=""LoginUser"" value=""""/>
    <add key=""LoginPwd"" value=""""/>
    <add key=""HideInputSoftON"" value=""false""/>
    <add key=""errlog"" value=""没错误""/>
    <add key=""HisData"" value=""""/>
    <add key=""ERPData"" value=""1""/>
  </appSettings>
</configuration>", "", "", "", "");
            Java.IO.BufferedWriter buf1 = new Java.IO.BufferedWriter(new Java.IO.OutputStreamWriter(fileoOS));
            buf1.Write(str, 0, str.Length);
            buf1.Flush();
            buf1.Close();
            return true;
        }

        private void AndroidEnvironment_UnhandledExceptionRaiser(object sender, RaiseThrowableEventArgs e)
        {
            Toast.MakeText(this, "错误:" + e.Exception.ToString(), ToastLength.Long).Show();
            e.Handled = true;
        }

        private bool GetPDAModuleLimit(List<ModuleLimit> moduleList, int iorder)
        {
            bool returnValue = false;
            try
            {
                if (moduleList.Where(c => c.iorder == iorder).Any())
                {
                    returnValue = moduleList.First(c => c.iorder == iorder).accesspermission;
                }
            }
            catch (Exception)
            {
                returnValue = false;
            }
            return returnValue;
        }
        private bool CheckIMEI()
        {
            bool returnValue = false;
            string Imei = GlobalDataCache.Imei;
            try
            {

                if (string.IsNullOrWhiteSpace(Imei))
                {
                    Toast.MakeText(this, "获取系统信息失败！" + Imei, ToastLength.Long).Show();
                    return returnValue;
                }
                var location = GlobalDataCache.FilePath;// System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                var fileName = System.IO.Path.Combine(location, "K9PDA.config");
                string EnKey = DESEncrypt.Encrypt(Imei);
                string EnKeyLocal = "";
                string strchkUDI = "false";
                if (System.IO.File.Exists(fileName))
                {
                    EnKeyLocal = GetValue(fileName, "AppKeyValue");
                    strchkUDI = GetValue(fileName, "chkUDI");
                }
                else
                {
                    EnKeyLocal = CheckImeiOnline(Imei);
                    if (EnKeyLocal.Contains('-'))
                    {
                        var data = EnKeyLocal.Split("-");
                        EnKeyLocal = data[0];
                    }
                }
                if (EnKey.ToUpper() == EnKeyLocal.ToUpper())
                {
                    string sContent =
 string.Format(@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <appSettings>
    <add key=""AppKeyValue"" value=""{0}""/>
    <add key=""HttpUrl"" value=""{1}""/>
    <add key=""chkUDI"" value=""{2}""/>
  </appSettings>
</configuration>", EnKey, GlobalDataCache.HttpUrl, strchkUDI);
                    CreateFile("K9PDA.config", sContent);
                    returnValue = true;
                }
            }
            catch (Exception ex)
            {
                SysLog.WriteLog("校验IMEI码[" + Imei + "]出错：" + ex.ToString(), true);
                returnValue = false;
            }
            return returnValue;
        }

        /// <summary>
        /// 获取配置值
        /// </summary>
        public string GetValue(string appPath, string appKey)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(AppSettings.AppConfig(appPath));
            XmlNode xNode;
            XmlElement xElem;
            xNode = xDoc.SelectSingleNode("//appSettings");
            //V1.2 原代码用单引号拼接 XPath，appKey 含单引号时导致解析异常
            //改用双引号包裹 key 值，避免单引号截断 XPath 表达式
            xElem = (XmlElement)xNode.SelectSingleNode($"//add[@key=\"{appKey}\"]");
            if (xElem != null)
                return xElem.GetAttribute("value");
            else
                return "";
        }

        public string CheckImeiOnline(string Imei)
        {
            string RetrunValue = "";
            try
            {
                //Imei = "2tfdc7gw8-QEClGFGuEJG6FWQiZuQcSQ";
                //string url = string.Format("http://192.168.10.211:6790/?do=pdacheck&an={0}&tk={1}&vn={2}", "CRM开发账套", "908f83a4c700ea90c0818159a0f433e2", Imei);
                //string url = string.Format("http://112.74.76.153:8092/?do=pdacheck&an={0}&tk={1}&vn={2}", "广州金博信息技术有限公司", "4a4ef96a0afa3cbcaf7a71d6b77bd0d4", Imei);
                //string url = string.Format("http://k9.wxkingbos.com:8095/?do=pdacheck&an={0}&tk={1}&vn={2}", "广州金博信息技术有限公司", "4a4ef96a0afa3cbcaf7a71d6b77bd0d4", Imei);
                var location = GlobalDataCache.FilePath;// System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                var fileName = System.IO.Path.Combine(location, "K9PDA.config");
                var httpurl = GlobalDataCache.HttpUrl;
                var httpurlconfig = "";
                if (System.IO.File.Exists(fileName))
                    httpurlconfig = GetValue(fileName, "HttpUrl");
                if (httpurlconfig != "" && httpurl != httpurlconfig)
                {
                    httpurl = httpurlconfig;
                }
                string url = string.Format("" + httpurl + "?do=pdacheck&an={0}&tk={1}&vn={2}", "金博软件", "534f0ea83c14243f13a3ea0db6158991", Imei);
                SysLog.WriteLog("url：" + url, false);
                var json = HttpRequestHelper.GetHttp(url);
                SysLog.WriteLog("报号校验返回：" + json, false);
                var result = JSONSerializer.Deserialize<PDACheck>(json);
                if (result.Status)
                {
                    RetrunValue = result.Code;
                }
            }
            catch (Exception ex)
            {
                SysLog.WriteLog("报号校验出错：" + ex.ToString(), true);
                throw;
            }
            return RetrunValue;
        }

        public bool CreateFile(string FileName, string Content)
        {
            var filePath = System.IO.Path.Combine(GlobalDataCache.FilePath, FileName);// "K9PDA.config"
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            //V1.2 原代码使用 WriteLineAsync 但未 await，using 块结束时流已关闭
            //导致文件内容可能为空或不完整，改为同步 WriteLine 彻底规避此问题
            using (var writer = File.CreateText(filePath))
            {
                writer.WriteLine(Content);
            }
            return true;
        }

        class PDACheck
        {
            public bool Status;
            public string Code;
            public string Msg;
        }

        [Obsolete]
        private void InitSystem()
        {
            if (Build.VERSION.SdkInt >= Build.VERSION_CODES.M)
            {
                if (this.CheckSelfPermission(Android.Manifest.Permission.WriteExternalStorage) != Android.Content.PM.Permission.Granted)
                {
                    Toast.MakeText(this, "请设置系统存储空间权限！", ToastLength.Short).Show();
                    Finish();
                    return;
                }
                if (this.CheckSelfPermission(Android.Manifest.Permission.ReadPhoneState) != Android.Content.PM.Permission.Granted)
                {
                    Toast.MakeText(this, "请设置系统电话状态权限！", ToastLength.Short).Show();
                    Finish();
                    return;
                }
            }

            GlobalDataCache.FilePath = this.GetExternalFilesDir(Android.OS.Environment.DirectoryPictures).AbsolutePath.Replace("Pictures", "");
            AndroidEnvironment.UnhandledExceptionRaiser += AndroidEnvironment_UnhandledExceptionRaiser;
            if (!AppSettings.ExistsFile("Setting.config"))
            {
                if (!CreateFile())
                {
                    Toast.MakeText(this, "创建配置文件失败！", 0).Show();
                    Finish();
                    return;
                }
            }

            TelephonyManager telephony = (TelephonyManager)GetSystemService(Context.TelephonyService);
            try
            {
                if (Build.VERSION.SdkInt < Build.VERSION_CODES.M)
                {
                    GlobalDataCache.Imei = telephony.DeviceId;
                }
                else if (Build.VERSION.SdkInt <= Build.VERSION_CODES.OMr1)
                {
                    /*8.1系统获取IMEI，DeviceID可能会获取到MEID*/
                    GlobalDataCache.Imei = telephony.Imei;
                }
                else
                {
                    GlobalDataCache.Imei = Android.Provider.Settings.System.GetString(this.ContentResolver, Android.Provider.Settings.Secure.AndroidId).ToUpper();
                }
            }
            catch (Exception ex)
            {
                GlobalDataCache.Imei = "";
            }
            Toast.MakeText(this, GlobalDataCache.Imei, ToastLength.Short).Show();
#if !DEBUG
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                if (!CheckIMEI())
                {
                    Intent intent = new Intent(this, typeof(RegisterActivity));
                    StartActivityForResult(intent, 10);
                    string errCode = GlobalDataCache.Imei.Length >= 5 ? GlobalDataCache.Imei.Remove(0, GlobalDataCache.Imei.Length - 5) : GlobalDataCache.Imei;
                    Toast.MakeText(this, "PDA尚未注册，请先注册后再使用！错误码：" + errCode, 0).Show();
                    //Toast.MakeText(this, "PDA尚未注册，请先注册后再使用！" + GlobalDataCache.Imei, 0).Show();
                    Finish();
                    return;
                }
            }
#endif
            GlobalDataCache.PDATRACELimitON = false;
            //GlobalDataCache.AppProductName = "K9PDA.K9TRACE";
            GlobalDataCache.AppProductName = BaseContext.PackageName.ToUpper();
            var hideInputSoftON = AppSettings.GetValue("Setting.config", "HideInputSoftON");
            GlobalDataCache.HideInputSoftON = string.IsNullOrWhiteSpace(hideInputSoftON) ? false : Convert.ToBoolean(hideInputSoftON);
            GlobalDataCache.PDALoginProduct = AppSettings.GetValue("Setting.config", "ERPData");
            if (string.IsNullOrEmpty(GlobalDataCache.PDALoginProduct)) GlobalDataCache.PDALoginProduct = "1";
        }


    }

    /// <summary>
    /// 全局焦点信息监听器
    /// </summary>
    public class FocusChangeListener : Java.Lang.Object, ViewTreeObserver.IOnGlobalFocusChangeListener
    {
        private readonly System.Action<View, View> _callback;
        public FocusChangeListener(System.Action<View, View> callback)
        {
            _callback = callback;
        }
        public void OnGlobalFocusChanged(View oldFocus, View newFocus)
        {
            if (oldFocus == null || newFocus == null) return;
            _callback?.Invoke(oldFocus, newFocus);
        }
    }

    internal class GlobalLayoutListener : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener
    {
        public View rootView;
        public EditText currentEditText;
        //public IntPtr Handle=>new IntPtr();

        //public void Dispose()
        //{
        //    this.Dispose();
        //}

        public void OnGlobalLayout()
        {
            Rect visibleArea = new Rect();
            rootView.GetWindowVisibleDisplayFrame(visibleArea);
            int screenHeight = rootView.Height;
            int keyboardHeight = screenHeight - visibleArea.Bottom;

            if (keyboardHeight > screenHeight * 0.15)
            {
                currentEditText.Focusable = true;
                currentEditText.FocusableInTouchMode = true;
                currentEditText.RequestFocus();
                currentEditText.FindFocus();
                //Console.WriteLine("键盘！！！！");
            }
            else
            {
                //Console.WriteLine("键盘无了！！！");
            }
        }
    }
}