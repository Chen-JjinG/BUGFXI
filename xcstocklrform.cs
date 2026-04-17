using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using K9PDA.Adapter;
using K9PDA.Infrastructure.Model;
using K9PDA.Infrastructure.Request;
using K9PDA.Infrastructure.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using static Android.App.ActionBar;
using static Android.Widget.AdapterView;
using System.Data;
using KingBos.Infrastructure.Model;
using ZXing.Mobile;
using System.Threading.Tasks;
using Android.Views.Animations;
using K9PDA.Customized.UIGenerateHelper;
using K9PDA.Infrastructure;

/* ------------------------------------------------
版本记录      版本日期      编辑人      编辑内容
V1.2        2025-08-05      CJJ       BUG#59662增加显示生产许可证
V1.3        2025-08-11      CJJ       UDI码采集相关优化
--------------------------------------------------- */

namespace K9PDA
{
    [Activity(Label = "xcstocklrform", ParentActivity = typeof(xcstockform))]
    public class xcstocklrform : BaseFrom, PopupWindow.IOnDismissListener, IOnItemClickListener
    {
        private LinearLayout linearlayout_info;
        private Button btnCollapse;

        private ListView listview;
        private TextView txtfqty;
        private TextView txt_scxkz;//V1.2
        private EditText txtprice;
        private EditText txtvalue;
        private EditText txtSCDATE;
        private EditText txtYXDATE;
        private EditText txtInput;
        private Button btnUncheck;
        private Button btncheck;
        private Button btnCommitcheck;
        private Button btnYCcheck;
        private Button btn_code_scan;
        private CheckBox fz_chk_sel;
        private XCSTOCKMainInfo XCSTOCKMainInfo;
        private XCSTOCKGoods MainGoodsInfo;
        private List<XCSTOCKGoods> resultList;
        private List<XCSTOCKGoods> resultList2;
        long seldialogId1 = -1;//获取当前验收的id1字段值
        long selDelId1 = -1;
        private List<string> listcph;
        private TextView txtcph;
        private List<XCSTOCKMainInfo> RKresultList;
        int mtype = 0;
        string cgoodsid;
        QRDealGoodsData infoQr = new QRDealGoodsData();
        private List<DataRow> dialogQRList;
        private List<string> rejectionReasons;  //拒收原因
        private DataRow drCurr;
        private List<DataRow> dsdataRows;

        #region 组合条码变量
        private List<GoodsTrace12> CurrentEditTrace12List;//当前操作的d12记录数据
        private List<GoodsTrace13> CurrentEditTrace13List;//当前操作的d13记录数据

        private GoodsTrace12 CurrentTempEditTrace12;//当前操作的d12记录数据
        private GoodsTrace13 CurrentTempEditTrace13;//当前操作的d13记录数据

        private bool bchkUDI;
        private CheckBox chkUDI;
        private List<DataRow> dialogUDIList;
        private CommonPopwindowAdapter<DataRow> popUDIWindowAdapter;
        List<InstrumentGoodsInfo> ChooseGoodsInfo = new List<InstrumentGoodsInfo>();
        private string _lastCode { get; set; }
        private string UDIlastCode;
        private int _lastUDIRuleLen { get; set; } //解析为UDI码规则的条码总长度
        private int UDIastUDIRuleLen;
        private string _Rulecode { get; set; } //规则编码
        private string _LastUDIgoodsid { get; set; }//前一个扫描选择的商品id
        private string UDIRulecode;

        private int scanCount = 1;//UDI码扫描次数
        #endregion

        private EditText txtFILE;
        private List<PDAGoodscfilenofilelistRequest> cfilenofilelist;//商品注册证选择
        private List<string> cfilenogoodfilelist;//商品注册证选择
        private bool IsEnableSFDARenewal;
        private SpinnerPopWindowAdapter<string> spinnerPopcfileno;


        private string sUserName = "";
        private string sUserId = "";
        private string sUserName2 = "";
        private string sUserId2 = "";
        string sCheckYsTitle = "";//核验人标题
        private DataTable dtTempData;

        private UDIScanSettingHelper udiScanSettingHelper;
        private string strcbiltype = "";//单据类型
        private int iSamePHProcess = 0;
        //拍照
        private bool bMobileScan;
        private bool IsUseNewScanFlow;//是否采码新流程
        MobileBarcodeScanner scanner;
        View zxingOverlay;

        private List<DataRow> phpopWindowList;

        #region 列表样式相关参数
        private SelectionAdapterWrapper<CKFHDGoodsInfoAdapter> _wrappedAdapter;
        private ListViewSelectionManager _selectionManager;
        private CKFHDGoodsInfoAdapter _originalAdapter;
        #endregion

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetTheme(Android.Resource.Style.ThemeLightNoTitleBar);
            this.Window.SetSoftInputMode(SoftInput.StateAlwaysHidden);
            SetContentView(Resource.Layout.xcstocklrform);
            ImageView ac = FindViewById<ImageView>(Resource.Id.actionMenuView1);
            ac.Click += TB_NavigationOnClick;

            XCSTOCKMainInfo = GlobalDataCache.GetData<XCSTOCKMainInfo>("PDACKFHDInfo");
            strcbiltype = XCSTOCKMainInfo.cbiltype;
            #region 组合条码

            var linearLayoout_option = FindViewById<LinearLayout>(Resource.Id.linearLayoout_option);
            fz_chk_sel = FindViewById<CheckBox>(Resource.Id.fz_chk_sel); //辅助条码

            this.chkUDI = FindViewById<CheckBox>(Resource.Id.checkUDI);//是否扫组合条码

            chkUDI.CheckedChange += (a, b) =>
            {
                bchkUDI = b.IsChecked;
                if (!bchkUDI)
                {
                    _lastCode = "";
                    ChooseGoodsInfo = new List<InstrumentGoodsInfo>();
                }
                DataTableHelper.GetConfigValue("chkUDI", true, bchkUDI ? "true" : "false");
                LinearLayoutCollapseHelper.CollapseExpand(linearLayoout_option, null, (int)(this.fz_chk_sel.MinHeight));
            };
            linearLayoout_option.Visibility = ViewStates.Gone;
            if (GlobalProxySetting.ConfigList != null && GlobalProxySetting.ConfigList.Where(a => (a.cparmname == "INSTRCODE") && (a.cparmvalue == "1")).Any())//是否启用UDI组合条码
            {
                chkUDI.Visibility = ViewStates.Visible;
                if (DataTableHelper.GetConfigValue("chkUDI") == "true")
                {
                    bchkUDI = true;
                    chkUDI.Checked = true;
                }
                else
                {
                    chkUDI.Checked = false;
                    linearLayoout_option.Visibility = ViewStates.Gone;
                }
            }
            else
            {
                chkUDI.Visibility = ViewStates.Gone;
                linearLayoout_option.Visibility = ViewStates.Gone;
            }

            #endregion
            listview = FindViewById<ListView>(Resource.Id.listView1);
            listview.ItemClick += ListView_ItemClick;
            listview.ItemsCanFocus = true;

            //txtfqty = FindViewById<EditText>(Resource.Id.txt_qty);
            //txtfqty.FocusChange += Txtfqty_FocusChange;
            //txtfqty.KeyPress += Txtfqty_KeyPress;
            //txtprice = FindViewById<EditText>(Resource.Id.txt_price);
            //txtprice.FocusChange += Txtprice_FocusChange;
            //txtprice.KeyPress += Txtprice_KeyPress;

            //txtvalue = FindViewById<EditText>(Resource.Id.txt_value);
            //txtvalue.FocusChange += Txtvalue_FocusChange;
            //txtvalue.KeyPress += Txtvalue_KeyPress;

            //txtSCDATE = FindViewById<EditText>(Resource.Id.txt_scrq);
            //txtSCDATE.KeyPress += TxtSCDATE_KeyPress;

            //txtYXDATE = FindViewById<EditText>(Resource.Id.txt_yxq);
            //txtYXDATE.KeyPress += TxtYXDATE_KeyPress;
            this.txt_scxkz = FindViewById<TextView>(Resource.Id.txt_scxkz);//V1.2
            txtInput = FindViewById<EditText>(Resource.Id.txtgoods);
            SetTextDraw(txtInput, Resource.Drawable.search_new, 0, 0, 7, 7);//添加搜索图标
            txtInput.KeyPress += (s, e) =>
            {
                TxtInput_KeyPressAsync(s, e);
            };
            this.txtfqty = FindViewById<TextView>(Resource.Id.txtfnoticeqty);
            var btnclearcheck = FindViewById<Button>(Resource.Id.btn_clearcheck);
            btnclearcheck.Click += btnclearcheck_Click;

            btncheck = FindViewById<Button>(Resource.Id.btn_check);
            btncheck.Click += Btncheck_Click;

            btnUncheck = FindViewById<Button>(Resource.Id.button1);
            btnUncheck.Click += BtnUncheck_Click;
            btnYCcheck = FindViewById<Button>(Resource.Id.button2);
            btnYCcheck.Click += BtnYCcheck_Click;
            btnCommitcheck = FindViewById<Button>(Resource.Id.button3);
            btnCommitcheck.Click += BtnCommitcheck_Click;
            btn_code_scan = FindViewById<Button>(Resource.Id.btn_code_scan);
            btn_code_scan.Click += Btn_code_scan_Click;
            IsUseNewScanFlow = GlobalProxySetting.ConfigList.Any(a => a.cparmname == "UseZsmNewProcess" && a.cparmvalue == "1");
            if (!IsUseNewScanFlow)
            {
                btn_code_scan.Visibility = ViewStates.Gone;
            }

            txtcph = FindViewById<TextView>(Resource.Id.txtcph);

            GlobalDataCache.SetData("REFRECKFHDMXLIST", new Action(() => { loadform(); }));

            this.txtFILE = FindViewById<EditText>(Resource.Id.txtcfile);
            SetTextDraw(txtFILE, Resource.Drawable.expand, 0, 0, 7, 7);//添加下拉图标
            IsEnableSFDARenewal = GlobalProxySetting.ConfigList != null && GlobalProxySetting.ConfigList.Where(a => (a.cparmname == "IsEnableSFDARenewal") && (a.cparmvalue == "1")).Any();
            if (GlobalDataCache.P_productTRADE == "器械")
            {
                FindViewById<TextView>(Resource.Id.txtviewcfile).Text = "注册证号：";
            }
            #region 拍照
            ImageButton btnmobilescan = FindViewById<ImageButton>(Resource.Id.btnphonescan);
            bMobileScan = GlobalProxySetting.ConfigList != null && GlobalProxySetting.ConfigList.Where(a => (a.cparmname == "PDAMobileScan") && (a.cparmvalue == "1")).Any();
            if (bMobileScan)
            {
                btnmobilescan.Visibility = ViewStates.Visible;
            }
            else
            {
                btnmobilescan.Visibility = ViewStates.Gone;
            }
            btnmobilescan.Click += Btnmobilescan_Click;
            #endregion
            if (GlobalProxySetting.ConfigList != null && GlobalProxySetting.ConfigList.Where(a => a.cparmname == "SamePHProcess").Any())
            {
                iSamePHProcess = ConvertHelper.ToInt(GlobalProxySetting.ConfigList.First(c => c.cparmname == "SamePHProcess").cparmvalue);//2024-06-15 出现相同批号，不同产期或有效期时处理
            }


            #region 详情信息折叠

            linearlayout_info = FindViewById<LinearLayout>(Resource.Id.linearlayout_info);
            btnCollapse = FindViewById<Button>(Resource.Id.btn_collapse);
            btnCollapse.Click += BtnCollapse_Click;

            #endregion

            #region new helperclass

            //V1.3 开启新流程才有此功能
            if (IsUseNewScanFlow)
            {
                udiScanSettingHelper = new UDIScanSettingHelper(this, linearLayoout_option);
            }

            TextViewToastTextHelper.ApplyGlobalClick(this);
            //var helper = new ListViewColorHelper<CKFHDGoodsInfoAdapter>(listview);
            #region 新版列表选择实现

            _selectionManager = new ListViewSelectionManager(multiSelect: false);
            listview.ItemClick += (sender, e) =>
            {
                _selectionManager.ToggleSelection(e.Position);
            };
            _originalAdapter = new CKFHDGoodsInfoAdapter(this, new List<XCSTOCKGoods>());
            _wrappedAdapter = new SelectionAdapterWrapper<CKFHDGoodsInfoAdapter>(
                _originalAdapter,
                _selectionManager,
                listview,
                selectedBackgroundColor: custom_background_selected_lite
                );

            listview.Adapter = _wrappedAdapter;

            #endregion

            #endregion

            loadform();
            //spSeasonInit();
        }

        /// <summary>
        /// 折叠展开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCollapse_Click(object sender, EventArgs e)
        {
            LinearLayoutCollapseHelper.CollapseExpand(linearlayout_info, btnCollapse);
        }

        /// <summary>
        /// 手机拍照
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btnmobilescan_Click(object sender, EventArgs e)
        {
            scanner = new MobileBarcodeScanner();
            Task t = new Task(AutoScan);
            t.Start();
        }
        #region 调用手机扫描
        async void AutoScan()
        {
            if (!bMobileScan) return;
            scanner.UseCustomOverlay = true;
            zxingOverlay = LayoutInflater.FromContext(this).Inflate(Resource.Layout.ZxingOverlay, null);

            ImageView ivScanning = zxingOverlay.FindViewById<ImageView>(Resource.Id.ivScanning);
            Button btnCancelScan = zxingOverlay.FindViewById<Button>(Resource.Id.btnCancelScan);
            ImageView actionMenuView1 = zxingOverlay.FindViewById<ImageView>(Resource.Id.actionMenuView1); //
            btnCancelScan.Click += (s, e) =>
            {
                if (scanner != null)
                {
                    scanner.Cancel();
                }
            };

            actionMenuView1.Click += (s, e) =>
            {
                if (scanner != null)
                {
                    scanner.Cancel();
                }
            };


            zxingOverlay.Measure(MeasureSpecMode.Unspecified.GetHashCode(), MeasureSpecMode.Unspecified.GetHashCode());
            int width = zxingOverlay.MeasuredWidth;
            int height = zxingOverlay.MeasuredHeight;

            // 从上到下的平移动画
            Animation verticalAnimation = new TranslateAnimation(0, 0, 0, height);
            verticalAnimation.Duration = 3000; // 动画持续时间
            verticalAnimation.RepeatCount = Animation.Infinite; // 无限循环

            // 播放动画
            ivScanning.Animation = verticalAnimation;
            verticalAnimation.StartNow();

            scanner.CustomOverlay = zxingOverlay;
            var mbs = MobileBarcodeScanningOptions.Default;
            mbs.AssumeGS1 = true;
            mbs.AutoRotate = true;
            mbs.DisableAutofocus = false;
            mbs.PureBarcode = false;
            mbs.TryInverted = true;
            mbs.TryHarder = true;
            mbs.UseCode39ExtendedMode = true;
            mbs.UseFrontCameraIfAvailable = false;
            mbs.UseNativeScanning = true;

            var result = await scanner.Scan(this, mbs);
            HandleScanResult(result);

        }

        private void HandleScanResult(ZXing.Result result)
        {
            // View.KeyEventArgs Data_Event = new View.KeyEventArgs(true, Keycode.Enter, null);


            if (result != null && !string.IsNullOrEmpty(result.Text))
            {
                if (result.Text != null && result.Text.Trim().Length > 0)
                {
                    this.RunOnUi(() =>
                    {

                        txtInput.Text = result.Text;
                        View.KeyEventArgs keys = new View.KeyEventArgs(true, Keycode.Enter, new KeyEvent(KeyEventActions.Up, Keycode.Enter));
                        TxtInput_KeyPressAsync(null, keys);
                    });

                }
                else
                {
                    //this.RunOnUi(() => { this.ShowToast("扫描无数据"); });
                    this.RunOnUi(() => { this.Proxy.MakeTextShow(this, "扫描无数据！", ToastLength.Short); ; });

                }
            }
            else
            {
                //this.RunOnUi(() => { this.ShowToast("扫描取消"); });
                this.RunOnUi(() => { this.Proxy.MakeTextShow(this, "扫描取消！", ToastLength.Short); ; });
            }
            scanner.Cancel();
        }
        #endregion
        private bool OnTouch(View v, MotionEvent e)
        {
            switch (e.Action)
            {
                case MotionEventActions.Down:
                    var right = (v as EditText).GetCompoundDrawables()[2];


                    this.txtInput.Focusable = true;
                    this.txtInput.FocusableInTouchMode = true;
                    this.txtInput.RequestFocus();
                    (v as EditText).ShowSoftInputOnFocus = true; //是否隐藏键盘输入

                    scanner = new MobileBarcodeScanner();
                    Task t = new Task(AutoScan);
                    t.Start();
                    break;
                default:
                    break;
            }
            return base.OnTouchEvent(e);
        }
        private void spSeasonInit()
        {
            this.Proxy.Execute(new PDASHOPRequest()
            {
                OPType = 5,
                EmpCode = GlobalProxySetting.GetLoginState().EmployeeCode,
                cbilid = XCSTOCKMainInfo.cbilid
            }, (response) =>
            {
                if (!response.IsError)
                {

                    //var rejectionReasons = response.RejectionReasonsList.Select(c => c.ccodetext).ToArray();
                    rejectionReasons = new List<string>();
                    rejectionReasons = response.RejectionReasonsList.Select(c => c.ccodetext).ToList();
                    rejectionReasons.Add("");
                }
            }, this);

        }
        /// <summary>
        /// 确认复核
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btncheck_Click(object sender, EventArgs e)
        {
            if (MainGoodsInfo == null || string.IsNullOrEmpty(MainGoodsInfo.cgoodsid))
            {
                return;
            }
            GlobalDataCache.SetData("CheckFHDetailGoods", MainGoodsInfo);
            Intent intent = new Intent(this, typeof(PDAxcstockCheckFH));

            //StartActivity(intent);
            StartActivityForResult(intent, 100);
        }
        /// <summary>
        /// 完成复核
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCommitcheck_Click(object sender, EventArgs e)
        {
            if (resultList != null && resultList.Count > 0)
            {
                var tmplist = resultList.Where(a => a.ffhqty > a.fnoticeqty).ToList();
                if (tmplist != null && tmplist.Count > 0)
                {
                    Toast.MakeText(this, "存在复核数量大于通知数量的数据，不允许完成复核！", 0).Show();
                    return;
                }
                for (int i = 0; i < resultList.Count; i++)
                {
                    if (ConvertHelper.ToString(resultList[i].dexpdate) != "")
                    {
                        if (Convert.ToDateTime(resultList[i].dexpdate) <= DateTime.Now)
                        {
                            Toast.MakeText(this, "商品【" + resultList[i].cgoodsid + "】的批号【" + resultList[i].cph + "】有效期已经过期！", 0).Show();
                            return;
                        }
                    }
                    if (resultList[i].fcancelqty > 0)
                    {
                        Toast.MakeText(this, "商品【" + resultList[i].cgoodsname + "(" + resultList[i].cgoodsid + ")】存在异常数据，请先进行确认！", 0).Show();
                        return;
                    }
                }
                if (resultList.Where(p => (p.isgspspecial == 1 && p.isptype == 1)).Any() && (string.IsNullOrEmpty(XCSTOCKMainInfo.chker2)))
                {
                    if (!YsrChedk())
                    {
                        return;
                    }
                    CustomAlertDialog cadSubmit = new CustomAlertDialog();
                    if (resultList.Where(a => ((a.finputqty + a.ffhqty) != a.fnoticeqty)).ToList().Any())
                    {
                        //cadSubmit.OKClick += CaCommitcheck_Click;
                        cadSubmit.AlertDialogShow(this, "复核数量与订单数量不一致，请确定是否继续复核？");
                    }
                    else
                    {
                        cadSubmit = new CustomAlertDialog();
                        cadSubmit.OKClick += CaCommitcheck_Click;
                        cadSubmit.AlertDialogShow(this, "是否完成复核？");
                    }
                }
                else
                {
                    CustomAlertDialog cadSubmit = new CustomAlertDialog();
                    if (resultList.Where(a => ((a.finputqty + a.ffhqty) != a.fnoticeqty)).ToList().Any())
                    {
                        //cadSubmit.OKClick += CaCommitcheck_Click;
                        cadSubmit.AlertDialogShow(this, "复核数量与订单数量不一致，请确定是否继续复核？");
                    }
                    else
                    {
                        cadSubmit = new CustomAlertDialog();
                        cadSubmit.OKClick += CaCommitcheck_Click;
                        cadSubmit.AlertDialogShow(this, "是否完成复核？");
                    }
                }
            }
        }

        /// <summary>
        /// 采码按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_code_scan_Click(object sender, EventArgs e)
        {
            //V1.3
            if (resultList != null && resultList.Count > 0)
            {
                this.Proxy.Execute(new PDAXCSTOCKOPRequest()
                {
                    OrgID = GlobalProxySetting.OrgID,
                    EmpCode = GlobalProxySetting.GetLoginState().EmployeeCode,// GlobalProxySetting.UserID,//KB023 2023-12-26 关联员工报错
                    OPType = 12,
                    cfher2 = XCSTOCKMainInfo.chker2,
                    cbilid = XCSTOCKMainInfo.cbilid,
                    LRInfoTrace12List = CurrentEditTrace12List,
                    LRInfoTrace13List = CurrentEditTrace13List,
                    LRInfoList = resultList,
                    cbiltype = strcbiltype
                },
            (response) =>
            {
                if (response != null && !response.IsError)
                {
                    if (!response.IsError)
                    {
                        ScanCode();
                    }
                    else
                    {
                        Toast.MakeText(this, "采码前保存失败：" + response.ErrorMessage, 0).Show();
                    }
                }
            },
            this);
            }



            #region oldType

            //if (!dataDic.ContainsKey("ctablename"))
            //{
            //    dataDic.Add("ctablename", "bl_shd");
            //}
            //GlobalDataCache.SetData("TraceCodeDataRow", dataDic);

            //#region 判断是否存在明细
            //var request = new BusinessRequest() { BusinessKey = "PDAScanCodeProcess" };
            //request.Parameters["opType"] = 1;
            //request.Parameters["cempid"] = GlobalProxySetting.GetLoginState().EmployeeCode;
            //request.Parameters["tableName"] = dataDic["ctablename"].ToString();
            //request.Parameters["cbilid"] = dataDic["cbilid"].ToString();
            //request.Parameters["queryText"] = string.Empty;

            //var res = this.Proxy.Execute(request);
            //if (res.IsError)
            //{
            //    Toast.MakeText(this, res.ErrorMessage, 0).Show();
            //    return;
            //}

            //if (!res.Result.ContainsKey("dt") || (res.Result["dt"] as DataTable)==null || (res.Result["dt"] as DataTable).Select().Length <= 0)
            //{
            //    Toast.MakeText(this, "请先录入商品保存后再采码！", 0).Show();
            //    return;
            //}
            //#endregion

            //Intent intent = new Intent(this, typeof(TraceCodeScanActivity));
            //StartActivity(intent);

            #endregion
        }

        //V1.3
        private void ScanCode()
        {
            var dataDic = ConvertHelper.ConvertObjToDic(XCSTOCKMainInfo);

            var cbilid = XCSTOCKMainInfo.cbilid;
            if (cbilid == null || string.IsNullOrEmpty(cbilid))
            {
                Toast.MakeText(this, "单号不能为空！", 0).Show();
                return;
            }

            var tabname = string.Empty;
            switch (cbilid.Substring(0, 2))
            {
                case "XC":
                    tabname = "bl_xcstock";
                    break;
                case "PC":
                    tabname = "bl_pcstock";
                    break;
                case "CC":
                    tabname = "bl_ccstock";
                    break;
                case "QC":
                    tabname = "bl_qcstock";
                    break;
                default:
                    break;
            }

            if (string.IsNullOrEmpty(tabname))
            {
                Toast.MakeText(this, "未匹配到对应表单！", 0).Show();
                return;
            }

            if (!dataDic.ContainsKey("ctablename"))
            {
                dataDic.Add("ctablename", tabname);
            }

            StartTraceCodeScan.Start(dataDic, GlobalProxySetting, this);
        }

        private bool YsrChedk(string flag = "")
        {
            var check1 = "";
            var check2 = "";
            sUserId = "";
            sUserId2 = "";
            sUserName = "";
            sUserName2 = "";
            sCheckYsTitle = "";
            bool bresult = false;
            var view = LayoutInflater.Inflate(Resource.Layout.PDACheckUser, null);

            var radio1 = view.FindViewById<RadioButton>(Resource.Id.check_radio1);
            var radio2 = view.FindViewById<RadioButton>(Resource.Id.check_radio2);
            var rgroup = view.FindViewById<RadioGroup>(Resource.Id.check);
            var txtcheck = view.FindViewById<EditText>(Resource.Id.txtUserName);
            var txtpwd = view.FindViewById<EditText>(Resource.Id.txtUserPwd);
            var lblpwd = view.FindViewById<TextView>(Resource.Id.textView2);
            var LinearLayout = view.FindViewById<LinearLayout>(Resource.Id.linearLayout);

            if (!resultList.Where(p => (p.isgspspecial == 1 && p.isptype == 1) || p.isgspprotein == 1).Any())
            {
                LinearLayout.Visibility = ViewStates.Gone;
            }
            XCSTOCKMainInfo.chker1 = GlobalProxySetting.UserID;//核验人1默认为登陆人员
            sUserId = GlobalProxySetting.UserID;
            if (!string.IsNullOrEmpty(XCSTOCKMainInfo.chker1))
            {
                radio1.Visibility = ViewStates.Gone;//录入验收人1后，不显示
            }
            if (!string.IsNullOrEmpty(XCSTOCKMainInfo.chker2))
            {
                radio2.Visibility = ViewStates.Gone;//录入验收人2后，不显示
            }
            var LinearLayout2 = view.FindViewById<LinearLayout>(Resource.Id.linearLayout2);

            if (!GlobalProxySetting.ConfigList.Where(a => (a.cparmname == "CheckNeedLogin") && (a.cparmvalue == "1")).Any())
            {
                //不用录入密码
                LinearLayout2.Visibility = ViewStates.Gone;
            }
            if (GlobalProxySetting.ConfigList.Where(a => (a.cparmname == "ISCHECKBILL") && (a.cparmvalue == "1")).Any())
            {
                if (!string.IsNullOrWhiteSpace(XCSTOCKMainInfo.chker1) && LinearLayout.Visibility == ViewStates.Gone)//已经录入验收人1时，则处理
                {
                    bresult = true;
                    return bresult;
                }
                else
                {
                    Action<object, EventArgs> actRadioClick = (s, e) =>
                    {
                        RadioButton rb = (RadioButton)s;
                        if (rb != null)
                        {
                            switch (rb.Text)
                            {
                                case "核验人1":
                                    check1 = sUserId;
                                    sCheckYsTitle = "核验人1";
                                    break;
                                case "核验人2":
                                    check2 = sUserId2;
                                    sCheckYsTitle = "核验人2";
                                    break;
                                default:
                                    break;
                            }
                        }
                        SetRadioImage(radio1);
                        SetRadioImage(radio2);
                        SetRadioSelImage(rb);
                    };
                    radio1.Click += (s, e) => { actRadioClick(s, e); };
                    radio2.Click += (s, e) => { actRadioClick(s, e); };
                    //txtcheck.KeyPress += (s, e) => { txtUserName_KeyPress(s, e); };
                    //txtcheck.FocusChange += (s, e) => { txtUserName_FocusChange(s, null); };

                    //txtpwd.KeyPress += (s, e) => { txtPWD_KeyPress(s, e); };
                    //txtpwd.FocusChange += (s, e) => { txtPWD_FocusChange(s, null); };

                    for (int i = 0; i < (rgroup as ViewGroup).ChildCount; i++)
                    {
                        View child = (rgroup as ViewGroup).GetChildAt(i);
                        if (child is RadioButton && (child as RadioButton).Visibility == ViewStates.Visible)
                        {
                            actRadioClick(child, null);
                            break;
                        }
                    }


                    Action<object, EventArgs> EditTxt = (s1, e1) =>
                    {
                        EditText txt = (EditText)s1;
                    };
                    txtcheck.KeyPress += (s1, e1) =>
                    {
                        txtUserName_KeyPress(s1, e1);
                        if (!string.IsNullOrWhiteSpace(txtcheck.Text))
                        {
                            txtpwd.RequestFocus();
                        }
                    };
                    txtcheck.FocusChange += (s1, e1) =>
                    {
                        txtUserName_FocusChange(s1, e1);
                        if (!string.IsNullOrWhiteSpace(txtcheck.Text))
                        {
                            txtpwd.RequestFocus();
                        }
                    };

                    //txtpwd.KeyPress += (s1, e1) => { txtPWD_KeyPress(s1, e1); };
                    //txtpwd.FocusChange += (s1, e1) => { txtPWD_FocusChange(s1, null); };


                    var alertDialog = new AlertDialog.Builder(this).SetTitle("选择复核人").SetView(view)
                        .SetNeutralButton("取消", new EventHandler<DialogClickEventArgs>((aaa, bbb) =>
                        {
                            return;
                        }))
                        .SetPositiveButton("确定", new EventHandler<DialogClickEventArgs>((aaa, bbb) =>
                        {

                            string strName = "";
                            string strId = "";

                            if (sCheckYsTitle == "核验人1")
                            {

                                strName = sUserName;
                                strId = sUserId;
                                if (flag == "pwd" && !string.IsNullOrEmpty(sUserName))
                                {
                                    txtcheck.Text = sUserName;
                                }
                            }
                            else if (sCheckYsTitle == "核验人2")
                            {
                                strName = sUserName2;
                                strId = sUserId2;
                                if (flag == "pwd" && !string.IsNullOrEmpty(sUserName2))
                                {
                                    txtcheck.Text = sUserName2;
                                }
                            }
                            else
                            {
                                return;
                            }

                            if (txtcheck.Text == "")
                            {
                                Toast.MakeText(this, "请输入复核人账号！", 0).Show();
                                txtcheck.RequestFocus();
                                if (!YsrChedk())
                                {
                                    return;
                                }
                            }
                            else
                            {
                                if (txtcheck.Text != strName)//录入工号不一致时，则需要验证工号是否存在
                                {
                                    var request = new BusinessRequest() { BusinessKey = "PDAJHYSRCHECKProcess" };
                                    request.Parameters["opType"] = "CheckEmpCode";
                                    request.Parameters["VerifyMode"] = 1;
                                    request.Parameters["EmpCode"] = strId;
                                    var response = this.Proxy.Execute(request);
                                    if (response.IsError)
                                    {
                                        txtcheck.Text = "";
                                        Toast.MakeText(this, "验证账号失败！" + response.ErrorMessage, 0).Show();
                                        txtcheck.RequestFocus();
                                        if (!YsrChedk())
                                        {
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        var dt = response.Result["empdt"] as DataTable;
                                        if (dt != null && dt.Rows.Count > 0)
                                        {
                                            if (sCheckYsTitle == "核验人1")
                                            {
                                                sUserId = dt.Rows[0]["cempid"].ToString();
                                                sUserName = dt.Rows[0]["cempname"].ToString();
                                                txtcheck.Text = sUserName;
                                            }
                                            else
                                            {
                                                sUserId2 = dt.Rows[0]["cempid"].ToString();
                                                sUserName2 = dt.Rows[0]["cempname"].ToString();
                                                txtcheck.Text = sUserName2;
                                            }

                                        }
                                    }
                                }
                                if (txtcheck.Text == "")
                                {
                                    Toast.MakeText(this, "请输入复核人账号工号！", 0).Show();
                                    txtcheck.RequestFocus();
                                    if (!YsrChedk())
                                    {
                                        return;
                                    }
                                }
                                if (LinearLayout2.Visibility != ViewStates.Gone)//验证密码
                                {
                                    #region 验证密码
                                    var request2 = new BusinessRequest() { BusinessKey = "PDAJHYSRCHECKProcess" };
                                    request2.Parameters["opType"] = "CheckPwd";
                                    request2.Parameters["pwd"] = txtpwd.Text;
                                    request2.Parameters["EmpCode"] = strId;
                                    request2.Parameters["VerifyMode"] = 1;
                                    var response2 = this.Proxy.Execute(request2);
                                    if (response2.IsError)
                                    {
                                        Toast.MakeText(this, "密码验证失败！" + response2.ErrorMessage, 0).Show();
                                        txtpwd.Text = "";
                                        txtpwd.RequestFocus();
                                        if (!YsrChedk("pwd"))
                                        {
                                            return;
                                        }
                                    }
                                    if (sCheckYsTitle == "核验人1")
                                    {
                                        check1 = strId;
                                    }
                                    else
                                    {
                                        check2 = strId;
                                    }
                                    if (LinearLayout.Visibility == ViewStates.Gone)//验收
                                    {
                                        XCSTOCKMainInfo.chker1 = check1;
                                        if (string.IsNullOrEmpty(XCSTOCKMainInfo.chker1))
                                        {
                                            Toast.MakeText(this, "核验人1不允许为空！", 0).Show();
                                            return;
                                        }
                                        bresult = true;
                                    }
                                    else
                                    {
                                        //双人验收
                                        if (sCheckYsTitle == "核验人1")
                                        {
                                            XCSTOCKMainInfo.chker1 = check1;
                                        }
                                        if (!string.IsNullOrWhiteSpace(XCSTOCKMainInfo.chker2) && !string.IsNullOrWhiteSpace(XCSTOCKMainInfo.chker1))
                                        {
                                            bresult = true;
                                        }
                                        else
                                        {
                                            if (!string.IsNullOrWhiteSpace(check2) && sCheckYsTitle == "核验人2" && string.IsNullOrWhiteSpace(XCSTOCKMainInfo.chker1))
                                            {
                                                XCSTOCKMainInfo.chker2 = check2;
                                            }
                                            if ((!string.IsNullOrWhiteSpace(check2) && sCheckYsTitle == "核验人2" && string.IsNullOrWhiteSpace(XCSTOCKMainInfo.chker1))
                                                || (!string.IsNullOrWhiteSpace(check1) && sCheckYsTitle == "核验人1" && string.IsNullOrWhiteSpace(XCSTOCKMainInfo.chker2)))
                                            {
                                                if (!YsrChedk())
                                                {
                                                    if (string.IsNullOrWhiteSpace(check2))
                                                    {
                                                        string errorMessage = string.Join(",", resultList.Where(p => (p.isgspspecial == 1 && p.isptype == 1) || p.isgspprotein == 1).Select(r => "[" + r.cgoodsid + "]")) + "为[特殊药品],特殊药品需要双人复核！";
                                                        if (sCheckYsTitle == "核验人2") Toast.MakeText(this, errorMessage, 0).Show();
                                                        return;
                                                    }
                                                }
                                            }
                                            //MainInfo.chker2 = check2;
                                            if (string.IsNullOrWhiteSpace(check2))
                                            {
                                                string errorMessage = string.Join(",", resultList.Where(p => (p.isgspspecial == 1 && p.isptype == 1) || p.isgspprotein == 1).Select(r => "[" + r.cgoodsid + "]")) + "为[特殊药品],特殊药品需要双人复核！";
                                                if (sCheckYsTitle == "核验人2") Toast.MakeText(this, errorMessage, 0).Show();
                                                txtcheck.RequestFocus();
                                                return;
                                            }
                                            if (sCheckYsTitle == "核验人2" && string.IsNullOrEmpty(check2))
                                            {
                                                Toast.MakeText(this, "复核人2不允许为空！", 0).Show();
                                                txtcheck.RequestFocus();
                                                return;
                                            }
                                            if (sCheckYsTitle == "核验人2" && XCSTOCKMainInfo.chker1 == check2)
                                            {
                                                Toast.MakeText(this, "复核人1与复核人2不能是同一个人员！", 0).Show();
                                                txtcheck.RequestFocus();
                                                return;
                                            }
                                            if (sCheckYsTitle == "核验人2" && !string.IsNullOrEmpty(check2)) XCSTOCKMainInfo.chker2 = check2;
                                            if (!string.IsNullOrWhiteSpace(XCSTOCKMainInfo.chker2) && !string.IsNullOrWhiteSpace(XCSTOCKMainInfo.chker1)) bresult = true;
                                        }
                                    }
                                    #endregion
                                }
                                else
                                {
                                    #region 不需要验证密码
                                    if (sCheckYsTitle == "核验人1")
                                    {
                                        check1 = strId;
                                    }
                                    else
                                    {
                                        check2 = strId;
                                    }
                                    //不需要验证密码
                                    if (LinearLayout.Visibility == ViewStates.Gone)//验收
                                    {
                                        XCSTOCKMainInfo.chker1 = check1;
                                        if (string.IsNullOrEmpty(XCSTOCKMainInfo.chker1))
                                        {
                                            Toast.MakeText(this, "复核人1不允许为空！", 0).Show();
                                            txtcheck.RequestFocus();
                                            return;
                                        }
                                        bresult = true;
                                    }
                                    else
                                    {
                                        //双人验收
                                        if (sCheckYsTitle == "核验人1")
                                        {
                                            XCSTOCKMainInfo.chker1 = check1;
                                        }
                                        if (!string.IsNullOrWhiteSpace(XCSTOCKMainInfo.chker2) && !string.IsNullOrWhiteSpace(XCSTOCKMainInfo.chker1))
                                        {
                                            bresult = true;

                                        }
                                        else
                                        {
                                            if (!string.IsNullOrWhiteSpace(check2) && sCheckYsTitle == "核验人2" && string.IsNullOrWhiteSpace(XCSTOCKMainInfo.chker1))
                                            {
                                                XCSTOCKMainInfo.chker2 = check2;
                                            }
                                            if ((!string.IsNullOrWhiteSpace(check2) && sCheckYsTitle == "核验人2" && string.IsNullOrWhiteSpace(XCSTOCKMainInfo.chker1))
                                                || (!string.IsNullOrWhiteSpace(check1) && sCheckYsTitle == "核验人1" && string.IsNullOrWhiteSpace(XCSTOCKMainInfo.chker2)))
                                            {
                                                if (!YsrChedk())
                                                {

                                                    if (string.IsNullOrWhiteSpace(check2))
                                                    {
                                                        string errorMessage = string.Join(",", resultList.Where(p => (p.isgspspecial == 1 && p.isptype == 1) || p.isgspprotein == 1).Select(r => "[" + r.cgoodsid + "]")) + "为[特殊药品],特殊药品需要双人复核！";
                                                        if (sCheckYsTitle == "核验人2") Toast.MakeText(this, errorMessage, 0).Show();
                                                        return;
                                                    }
                                                }
                                            }

                                            if (sCheckYsTitle == "核验人2" && string.IsNullOrWhiteSpace(check2))
                                            {
                                                string errorMessage = string.Join(",", resultList.Where(p => (p.isgspspecial == 1 && p.isptype == 1) || p.isgspprotein == 1).Select(r => "[" + r.cgoodsid + "]")) + "为[特殊药品],特殊药品需要双人复核！";
                                                if (sCheckYsTitle == "核验人2") Toast.MakeText(this, errorMessage, 0).Show();
                                                return;
                                            }
                                            if (sCheckYsTitle == "核验人2" && string.IsNullOrEmpty(check2))
                                            {
                                                Toast.MakeText(this, "复核人2不允许为空！", 0).Show();
                                                txtcheck.RequestFocus();
                                                return;
                                            }

                                            if (sCheckYsTitle == "核验人2" && XCSTOCKMainInfo.chker1 == check2)
                                            {
                                                Toast.MakeText(this, "复核人1与复核人2不能是同一个人员！", 0).Show();
                                                txtcheck.RequestFocus();
                                                return;
                                            }
                                            if (sCheckYsTitle == "核验人2") XCSTOCKMainInfo.chker2 = check2;
                                            if (!string.IsNullOrWhiteSpace(XCSTOCKMainInfo.chker2) && !string.IsNullOrWhiteSpace(XCSTOCKMainInfo.chker1)) bresult = true;
                                        }
                                    }
                                    #endregion
                                }
                            }

                        }
                        ))
                        .Create();
                    alertDialog.SetView(view, 0, 0, 0, 0);
                    alertDialog.Show();
                }
            }
            else
            {
                bresult = true;
            }
            return bresult;
        }

        private void SetRadioImage(RadioButton radio)
        {
            Drawable dwRadio = Resources.GetDrawable(Resource.Drawable.colored_radio_btn_select);
            dwRadio.SetBounds(0, 0, dwRadio.MinimumWidth, dwRadio.MinimumHeight);
            radio.SetCompoundDrawables(dwRadio, null, null, null);
        }
        private void SetRadioSelImage(RadioButton radio)
        {
            Drawable dwRadio = Resources.GetDrawable(Resource.Drawable.colored_radio_btn_selected);
            dwRadio.SetBounds(0, 0, dwRadio.MinimumWidth, dwRadio.MinimumHeight);
            radio.SetCompoundDrawables(dwRadio, null, null, null);
        }
        private void txtUserName_KeyPress(object sender, View.KeyEventArgs e)
        {
            var view = LayoutInflater.Inflate(Resource.Layout.PDACheckUser, null);
            var txtpwd = view.FindViewById<EditText>(Resource.Id.txtUserPwd);
            EditText txtcheck = (EditText)sender;
            string sName = "";
            if (sCheckYsTitle == "核验人1")
            {
                sName = sUserName;
            }
            else if (sCheckYsTitle == "核验人2")
            {
                sName = sUserName2;
            }
            else
            {
                return;
            }
            e.Handled = false;

            if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Up)
            {
                var txt = txtcheck.Text.Trim();

                if (string.IsNullOrWhiteSpace(txt))
                {
                    Toast.MakeText(this, "请输入核验人账号！", 0).Show();
                    //txtcheck.RequestFocus();
                    e.Handled = true;
                    return;
                }
                if (txt != sName)
                {
                    var request = new BusinessRequest() { BusinessKey = "PDAJHYSRCHECKProcess" };
                    request.Parameters["opType"] = "CheckEmpCode";
                    request.Parameters["EmpCode"] = txt;
                    request.Parameters["VerifyMode"] = 1;
                    var response = this.Proxy.Execute(request);
                    if (response.IsError)
                    {
                        txtcheck.Text = "";
                        Toast.MakeText(this, "验证账号失败！" + response.ErrorMessage, 0).Show();
                        //txtcheck.RequestFocus();
                        e.Handled = true;
                        return;
                    }
                    else
                    {
                        var dt = response.Result["empdt"] as DataTable;
                        if (dt != null && dt.Rows.Count > 0)
                        {
                            if (sCheckYsTitle == "核验人1")
                            {
                                sUserId = dt.Rows[0]["cempid"].ToString();
                                sUserName = dt.Rows[0]["cempname"].ToString();
                                txtcheck.Text = sUserName;
                            }
                            else if (sCheckYsTitle == "核验人2")
                            {
                                sUserId2 = dt.Rows[0]["cempid"].ToString();
                                sUserName2 = dt.Rows[0]["cempname"].ToString();
                                txtcheck.Text = sUserName2;
                            }
                            //txtpwd.RequestFocus();
                        }

                    }
                }
                e.Handled = true;
            }
        }

        private void txtPWD_KeyPress(object sender, View.KeyEventArgs e)
        {

            var view = LayoutInflater.Inflate(Resource.Layout.PDACheckUser, null);
            var txtcheck = view.FindViewById<EditText>(Resource.Id.txtUserName);
            EditText txtpwd = (EditText)sender;
            e.Handled = false;

            if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Up)
            {
                if (string.IsNullOrWhiteSpace(txtcheck.Text))
                {
                    //txtpwd.ClearFocus();
                    txtcheck.Focusable = true;
                    txtcheck.FocusableInTouchMode = true;
                    txtcheck.RequestFocus();
                    e.Handled = true;
                    return;
                }
            }
            e.Handled = true;
        }
        private void txtPWD_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            var view = LayoutInflater.Inflate(Resource.Layout.PDACheckUser, null);
            var txtcheck = view.FindViewById<EditText>(Resource.Id.txtUserName);
            EditText txtpwd = (EditText)sender;

            var txt = txtcheck.Text.Trim();
            if (string.IsNullOrWhiteSpace(txt))
            {
                //txtpwd.ClearFocus();
                txtcheck.Focusable = true;
                txtcheck.FocusableInTouchMode = true;
                txtcheck.RequestFocus();
                return;
            }
        }
        private void txtUserName_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            var view = LayoutInflater.Inflate(Resource.Layout.PDACheckUser, null);
            var txtpwd = view.FindViewById<EditText>(Resource.Id.txtUserPwd);
            EditText txtcheck = (EditText)sender;
            string sName = "";
            if (sCheckYsTitle == "核验人1")
            {
                sName = sUserName;
            }
            else if (sCheckYsTitle == "核验人2")
            {
                sName = sUserName2;
            }
            else
            {
                return;
            }

            var txt = txtcheck.Text.Trim();

            if (!string.IsNullOrWhiteSpace(txt) && txt != sName)
            {
                var request = new BusinessRequest() { BusinessKey = "PDAJHYSRCHECKProcess" };
                request.Parameters["opType"] = "CheckEmpCode";
                request.Parameters["EmpCode"] = txt;
                request.Parameters["VerifyMode"] = 1;
                var response = this.Proxy.Execute(request);
                if (response.IsError)
                {
                    txtcheck.Text = "";
                    Toast.MakeText(this, "验证账号失败！" + response.ErrorMessage, 0).Show();
                    //txtcheck.RequestFocus();
                    return;
                }
                else
                {
                    var dt = response.Result["empdt"] as DataTable;
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        if (sCheckYsTitle == "核验人1")
                        {
                            sUserId = dt.Rows[0]["cempid"].ToString();
                            sUserName = dt.Rows[0]["cempname"].ToString();
                            txtcheck.Text = sUserName;
                        }
                        else if (sCheckYsTitle == "核验人2")
                        {
                            sUserId2 = dt.Rows[0]["cempid"].ToString();
                            sUserName2 = dt.Rows[0]["cempname"].ToString();
                            txtcheck.Text = sUserName2;
                        }
                        //txtpwd.RequestFocus();
                    }

                }
            }
        }
        /// <summary>
        /// 复核出库单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CaCommitcheck_Click(object sender, EventArgs e)
        {
            if (resultList != null && resultList.Count > 0)
            {
                this.Proxy.Execute(new PDAXCSTOCKOPRequest()
                {
                    OrgID = GlobalProxySetting.OrgID,
                    EmpCode = GlobalProxySetting.GetLoginState().EmployeeCode,// GlobalProxySetting.UserID,//KB023 2023-12-26 关联员工报错
                    OPType = 10,
                    cfher2 = XCSTOCKMainInfo.chker2,
                    cbilid = XCSTOCKMainInfo.cbilid,
                    LRInfoTrace12List = CurrentEditTrace12List,
                    LRInfoTrace13List = CurrentEditTrace13List,
                    LRInfoList = resultList,
                    cbiltype = strcbiltype
                },
            (response) =>
            {
                if (response != null && !response.IsError)
                {
                    if (!response.IsError)
                    {
                        Toast.MakeText(this, "复核完成！", 0).Show();
                        deleteckfh_data();
                        TB_NavigationOnClick(null, null);
                        GlobalDataCache.GetData<Action>("REFRECKFHDLIST")();
                    }
                    else
                    {
                        Toast.MakeText(this, "保存失败", 0).Show();
                    }
                }
            },
            this);
            }
        }
        /// <summary>
        /// 异常复核
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnYCcheck_Click(object sender, EventArgs e)
        {
            var ifinputChk = 0;

            if (resultList == null || resultList.Count <= 0)
            {
                Toast.MakeText(this, "没有要确认的异常数据！", 0).Show();
                return;
            }

            var finputChkList = resultList.Where(a => a.fnoticeqty != (a.finputqty + a.ffhqty)).ToList();
            if (finputChkList != null && finputChkList.Count > 0)
            {
                ifinputChk = finputChkList.Count;
            }
            var CancelChkList = resultList.Where(a => a.fcancelqty != 0).ToList();
            var funqty = ConvertHelper.ToDecimal(resultList.Where(a => a.fnoticeqty > 0).Sum(p => (p.fnoticeqty - p.ffhqty - p.finputqty)));

            //if (CancelChkList == null || CancelChkList.Count == 0)
            //{
            //    Toast.MakeText(this, "没有要确认的异常数据！", 0).Show();
            //    return;
            //}
            if (ifinputChk > 0)
            {
                CaYCcheck_Click(null, null);
                //CustomAlertDialog cadSubmit = new CustomAlertDialog();
                //cadSubmit.OKClick += CaYCcheck_Click;
                //cadSubmit.AlertDialogShow(this, string.Format("存在未复核的数据有（{0}）条！是否将未复核的数据异常复核成0数量？", ifinputChk.ToString()));
            }

            if (funqty <= 0)
            {
                Toast.MakeText(this, "本商品已完成复核，若需调整复核数量，请删除已复核数据，重新复核！", 0).Show();
                return;
            }


        }
        /// <summary>
        /// 异常处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CaYCcheck_Click(object sender, EventArgs e)
        {
            if (resultList == null || resultList.Count == 0)
            {
                Toast.MakeText(this, "没有商品明细信息，不需要进行异常复核！", 0).Show();
                return;
            }

            var resultdfhList = resultList.Where(a => a.fnoticeqty != (a.ffhqty + a.finputqty)).ToList();
            if (resultdfhList == null || resultdfhList.Count == 0)
            {
                Toast.MakeText(this, "商品已复核完成，不需要再进行异常复核！", 0).Show();
                return;
            }
            var dt = DataTableHelper.ToDataTabel<XCSTOCKGoods>(resultdfhList);
            var goodsList = dt.Select().ToList();
            dsdataRows = goodsList;
            drCurr = goodsList.FirstOrDefault();
            var cbilid = MainGoodsInfo != null ? MainGoodsInfo.cbilid : "";
            int iposition = -1;
            #region
            //  var view = LayoutInflater.Inflate(Resource.Layout.PDAxcstockYCFH, null);
            //  var layoutID = Resource.Layout.PDAxcstockYCGoodsFormListItem;
            //  var adapter = new ListViewCommonAdapter<DataRow>(this, goodsList, layoutID);
            //  var cbilid = MainGoodsInfo!=null?  MainGoodsInfo.cbilid:"";
            //  adapter.OnGetView += (position, convertView, parent, item, viewHolder) =>
            //  {
            //      Action<Color> action = (color) =>
            //      {
            //          viewHolder.GetView<TextView>(Resource.Id.textView1).SetTextColor(color);
            //          viewHolder.GetView<TextView>(Resource.Id.textView2).SetTextColor(color);
            //          viewHolder.GetView<TextView>(Resource.Id.textView3).SetTextColor(color);
            //          viewHolder.GetView<TextView>(Resource.Id.textView4).SetTextColor(color);
            //          viewHolder.GetView<TextView>(Resource.Id.textView5).SetTextColor(color);
            //      };
            //      viewHolder.SetText(Resource.Id.textView1, item["ccommonname"].ToString());
            //      viewHolder.SetText(Resource.Id.textView2, item["cph"].ToString());
            //      viewHolder.SetText(Resource.Id.textView3, item["fnoticeqty"].ToString());
            //      viewHolder.SetText(Resource.Id.textView4, item["ffhqty"].ToString());
            //      viewHolder.SetText(Resource.Id.textView5, (ConvertHelper.ToDecimal(item["fnoticeqty"])-ConvertHelper.ToDecimal(item["ffhqty"])).ToString());

            //      return viewHolder.GetConvertView();
            //  };
            //  var lvMain = view.FindViewById<ListView>(Resource.Id.listView1);
            //  lvMain.Adapter = adapter;
            //  var lcbilid = view.FindViewById<TextView>(Resource.Id.txtcbilid);
            //  lcbilid.Text = cbilid;//显示单号
            //  lvMain.ItemsCanFocus = true;
            //  int iposition = -1;
            //  lvMain.ItemClick += (a, b) =>
            //  {
            //      var tagValue = b.View.Tag as ListViewCommonAdapterViewHolder;
            //      iposition = tagValue.GetPosition();
            //      drCurr = dsdataRows[iposition];
            //      (lvMain.Adapter as ListViewCommonAdapter<DataRow>).NotifyDataSetChanged();
            //  };
            //  var alertDialog = new AlertDialog.Builder(this).SetTitle("异常复核确认").SetView(view)
            //  .SetNeutralButton("取消", new EventHandler<DialogClickEventArgs>((aaa, bbb) => {

            //  }
            //))
            // .SetPositiveButton("确定", new EventHandler<DialogClickEventArgs>((aaa, bbb) => {

            //     for (int i = 0; i < resultdfhList.Count; i++)
            //     {
            //         for (int j = 0; j < resultList.Count; j++)   
            //         {
            //             if (resultdfhList[i].id1 == resultList[j].id1)
            //             {
            //                 resultList[j].finputqty = resultList[j].fnoticeqty - resultList[j].ffhqty;
            //                 resultList[j].fcancelqty = 0;
            //                 GlobalDataCache.SetData("PDACKFHDGoodsInfo", resultList[j]);
            //                 break;
            //             }
            //         }
            //     }
            //     SetNewAdapter(new CKFHDGoodsInfoAdapter(this, resultList));

            // }
            //))
            //  .Create();
            //  alertDialog.SetView(view, 0, 0, 0, 0);
            //  alertDialog.Show();
            #endregion

            var builder = new Dialog(new ContextThemeWrapper(this, Resource.Style.CustomAlertDialogStyle));
            LayoutInflater layoutInflater = LayoutInflater.From(this);
            var alertview = layoutInflater.Inflate(Resource.Layout.xcstockycfhqr, null, false);
            builder.SetContentView(alertview);
            builder.Window.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
            builder.SetCancelable(false);

            var txtcbilid = alertview.FindViewById<TextView>(Resource.Id.txtcbilid);
            txtcbilid.Text = cbilid;
            var adapter = new ListViewCommonAdapter<DataRow>(this, goodsList, Resource.Layout.PDAxcstockYCGoodsFormListItem);
            adapter.OnGetView += (position, convertView, parent, item, viewHolder) =>
            {
                Action<Color> action = (color) =>
                {
                    viewHolder.GetView<TextView>(Resource.Id.textView1).SetTextColor(color);
                    viewHolder.GetView<TextView>(Resource.Id.textView2).SetTextColor(color);
                    viewHolder.GetView<TextView>(Resource.Id.textView3).SetTextColor(color);
                    viewHolder.GetView<TextView>(Resource.Id.textView4).SetTextColor(color);
                    viewHolder.GetView<TextView>(Resource.Id.textView5).SetTextColor(color);
                };
                viewHolder.SetText(Resource.Id.textView1, item["ccommonname"].ToString());
                viewHolder.SetText(Resource.Id.textView2, item["cph"].ToString());
                viewHolder.SetText(Resource.Id.textView3, ConvertHelper.ToDecimal(item["fnoticeqty"]).ToString(GlobalDataCache.QtyFormat));
                viewHolder.SetText(Resource.Id.textView4, ConvertHelper.ToDecimal(item["ffhqty"]).ToString(GlobalDataCache.QtyFormat));
                viewHolder.SetText(Resource.Id.textView5, (ConvertHelper.ToDecimal(item["fnoticeqty"]) - ConvertHelper.ToDecimal(item["ffhqty"])).ToString(GlobalDataCache.QtyFormat));

                return viewHolder.GetConvertView();
            };
            var lvMain = alertview.FindViewById<ListView>(Resource.Id.listView1);
            lvMain.Adapter = adapter;
            lvMain.ItemClick += (a, b) =>
            {
                var tagValue = b.View.Tag as ListViewCommonAdapterViewHolder;
                iposition = tagValue.GetPosition();
                drCurr = dsdataRows[iposition];
            };
            var helper = new ListViewColorHelper<ListViewCommonAdapter<DataRow>>(lvMain);
            alertview.FindViewById(Resource.Id.btn_ok).Click += (s, ev) =>
            {
                for (int i = 0; i < resultdfhList.Count; i++)
                {
                    for (int j = 0; j < resultList.Count; j++)
                    {
                        if (resultdfhList[i].id1 == resultList[j].id1)
                        {
                            resultList[j].finputqty = resultList[j].fnoticeqty - resultList[j].ffhqty;
                            resultList[j].fcancelqty = 0;
                            GlobalDataCache.SetData("PDACKFHDGoodsInfo", resultList[j]);
                            break;
                        }
                    }
                }
                _originalAdapter.UpdateData(resultList);
                //SetNewAdapter(new CKFHDGoodsInfoAdapter(this, resultList));
                builder.Dismiss();
            };
            alertview.FindViewById(Resource.Id.btn_cancel).Click += (s, ev) =>
            {
                builder.Dismiss();
            };
            builder.Show();

        }
        //计算扫描数量
        private decimal CoumTracefqty(XCSTOCKGoods info, string flag, ref bool bResult)
        {
            decimal dtmpfqty = 0;
            decimal dtmpfqty2 = 0;
            bool Notctracename = false;
            bResult = false;
            if (bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
            {
                if (CurrentEditTrace12List == null) CurrentEditTrace12List = new List<GoodsTrace12>();
                if (CurrentEditTrace13List == null) CurrentEditTrace13List = new List<GoodsTrace13>();

                if (IsUseNewScanFlow)
                {
                    dtmpfqty = CurrentEditTrace12List.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph == ChooseGoodsInfo[0].cph && a.id1 == info.id1).Sum(p => p.fzsmqty);
                    scanCount = udiScanSettingHelper.ScanCount;
                    //采码的数量
                    //if (udiScanSettingHelper.IsBatchEnabled)
                    //{
                    dtmpfqty2 = ChooseGoodsInfo[0].fqty * scanCount;
                    //}
                    //else
                    //{
                    //    dtmpfqty2 = ChooseGoodsInfo[0].fqty;
                    //}
                    dtmpfqty += dtmpfqty2;
                }
                else
                {
                    dtmpfqty2 = ChooseGoodsInfo[0].fqty;
                    if (ChooseGoodsInfo[0].itype == 1)////UDI码处理
                    {
                        //已经存在，则不需要修改数量//UDI存在时判断是否有相同序列号
                        if (ChooseGoodsInfo[0].childrentracecode != null && ChooseGoodsInfo[0].childrentracecode.Count > 0)
                        {
                            //if CurrentEditTrace12List.Where(a=>a.ctracename == ChooseGoodsInfo[0].childrentracecode.ForEach(y=>);
                            foreach (var child in ChooseGoodsInfo[0].childrentracecode)
                            {
                                //if (CurrentEditTrace12List != null && CurrentEditTrace12List.Count > 0 && CurrentEditTrace12List.Where(a => a.ctracename == child).Any())
                                //{
                                //    bResult = true;
                                //}
                                if (CurrentEditTrace13List != null && CurrentEditTrace13List.Count > 0 && CurrentEditTrace13List.Where(a => a.ctracename == ChooseGoodsInfo[0].cudicode).Any())
                                {
                                    bResult = true;
                                }
                            }
                        }
                        //else
                        //{
                        //    //无序列号
                        //    Notctracename = true;
                        //}
                        Notctracename = true;//UDI通用处理

                        if (bResult)
                        {
                            //dtmpfqty = CurrentEditTrace12List.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph == ChooseGoodsInfo[0].cph).Sum(p => p.fzsmqty);
                            dtmpfqty = ConvertHelper.ToDecimal(info.ffhqty);
                        }
                        else if (Notctracename)
                        {
                            if (flag == "check")
                            {
                                dtmpfqty = ConvertHelper.ToDecimal(info.ffhqty) + dtmpfqty2;
                            }
                            else
                            {
                                bResult = true;
                                if (info.fqty < ConvertHelper.ToDecimal(info.ffhqty))
                                {
                                    dtmpfqty = info.fqty;
                                }
                                else
                                {
                                    dtmpfqty = ConvertHelper.ToDecimal(info.ffhqty);
                                }
                                return dtmpfqty;
                            }
                        }
                        else
                        {
                            //dtmpfqty = CurrentEditTrace12List.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph == ChooseGoodsInfo[0].cph).Sum(p => p.fzsmqty) + dtmpfqty2;
                            dtmpfqty = ConvertHelper.ToDecimal(info.ffhqty) + dtmpfqty2;
                        }

                    }
                    else
                    {
                        if (CurrentEditTrace12List != null && iSamePHProcess > 1 && CurrentEditTrace12List.Any(p => p.cgoodsid == ChooseGoodsInfo[0].cgoodsid && p.cph == info.cph && p.id1 == info.id1 && p.cparenttrace == ChooseGoodsInfo[0].cudicode))
                        {
                            bResult = true;
                            //已经存在，则不需要修改数量
                        }
                        if (CurrentEditTrace12List != null && iSamePHProcess <= 1 && CurrentEditTrace12List.Any(p => p.cgoodsid == ChooseGoodsInfo[0].cgoodsid && p.cph == ChooseGoodsInfo[0].cph && p.id1 == info.id1 && p.ctracename == ChooseGoodsInfo[0].ctracecode))
                        {
                            bResult = true;
                            //已经存在，则不需要修改数量
                        }
                        if (bResult)
                        {
                            if (iSamePHProcess > 1)
                                dtmpfqty = CurrentEditTrace12List.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph == info.cph && a.cparenttrace == ChooseGoodsInfo[0].cudicode && a.id1 == info.id1).Sum(p => p.fzsmqty);
                            else
                                dtmpfqty = CurrentEditTrace12List.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph == ChooseGoodsInfo[0].cph && a.id1 == info.id1).Sum(p => p.fzsmqty);
                        }
                        else
                        {
                            if (iSamePHProcess > 1)
                                dtmpfqty = CurrentEditTrace12List.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph == info.cph && a.cparenttrace == ChooseGoodsInfo[0].cudicode && a.id1 == info.id1).Sum(p => p.fzsmqty) + dtmpfqty2;
                            else
                                dtmpfqty = CurrentEditTrace12List.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph == ChooseGoodsInfo[0].cph && a.id1 == info.id1).Sum(p => p.fzsmqty) + dtmpfqty2;
                        }
                    }
                }
            }
            if (bResult && flag == "check")
            {
                Toast.MakeText(this, "组合条码重复扫描，不允许此操作！", 0).Show();
            }
            return dtmpfqty;
        }
        private void AddCurrentEditTraceList(ref XCSTOCKGoods refInfo)
        {
            var info = refInfo;
            if (bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
            {

                if (IsUseNewScanFlow)
                {
                    //if (udiScanSettingHelper.IsBatchEnabled)
                    //{
                    for (int i = 0; i < scanCount; i++)
                    {
                        AddCurrentEditTraceListNewFlow(info);
                    }
                    //}
                    //else
                    //{
                    //    AddCurrentEditTraceListNewFlow(info);
                    //}
                    //Txt
                }
                else
                {
                    if (CurrentEditTrace12List == null) CurrentEditTrace12List = new List<GoodsTrace12>();
                    if (CurrentEditTrace13List == null) CurrentEditTrace13List = new List<GoodsTrace13>();
                    if (ChooseGoodsInfo[0].itype == 1)////UDI码处理
                    {
                        if (!string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].cudicode))
                        {
                            #region d13记录UDI码
                            var trace13 = new GoodsTrace13();
                            trace13.cbilid = XCSTOCKMainInfo.cbilid;
                            trace13.id1 = info.id1;
                            long Iid13 = 0;
                            if (CurrentEditTrace13List != null && CurrentEditTrace13List.Count > 0)
                            {
                                for (int i = 0; i < CurrentEditTrace13List.Count; i++)
                                {
                                    if (ConvertHelper.ToLong(CurrentEditTrace13List[i].id13) > Iid13)
                                    {
                                        Iid13 = ConvertHelper.ToLong(CurrentEditTrace13List[i].id13);
                                    }
                                }
                            }
                            trace13.id13 = Iid13 + 1;
                            trace13.cgoodsid = ChooseGoodsInfo[0].cgoodsid;
                            trace13.cph = (iSamePHProcess > 1 ? info.cph : ChooseGoodsInfo[0].cph);
                            trace13.dmadedate = (iSamePHProcess > 1 ? info.dmadedate : ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate));
                            trace13.dexpdate = (iSamePHProcess > 1 ? info.dexpdate : ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate));
                            trace13.cmjph = "";
                            trace13.ctracename = ChooseGoodsInfo[0].cudicode;
                            trace13.cirter = GlobalProxySetting.GetLoginState().EmployeeCode;
                            trace13.falotqty = ChooseGoodsInfo[0].fqty;
                            trace13.cnote = info.cbilid;
                            //2023-05-06 fmtp、fltp、fqty、cbzjb
                            trace13.fmtp = ChooseGoodsInfo[0].fmtp;
                            trace13.fltp = ChooseGoodsInfo[0].fltp;
                            trace13.fqty = ChooseGoodsInfo[0].fqty;
                            trace13.cbzjb = ChooseGoodsInfo[0].cbzjb;
                            CurrentTempEditTrace13 = trace13;
                            if (CurrentEditTrace13List != null && CurrentEditTrace13List.Any(p => p.cgoodsid == ChooseGoodsInfo[0].cgoodsid && p.cph == trace13.cph && p.ctracename == ChooseGoodsInfo[0].cudicode))
                            {
                                for (int i = 0; i < CurrentEditTrace13List.Count(); i++)
                                {
                                    if (CurrentEditTrace13List[i].cgoodsid == ChooseGoodsInfo[0].cgoodsid && CurrentEditTrace13List[i].cph == trace13.cph && CurrentEditTrace13List[i].ctracename == ChooseGoodsInfo[0].cudicode)
                                    {
                                        trace13.id1 = CurrentEditTrace13List[i].id1;
                                        trace13.id13 = CurrentEditTrace13List[i].id13;
                                        CurrentEditTrace13List[i] = trace13;
                                        CurrentTempEditTrace13 = trace13;
                                        break;
                                    }
                                }
                            }
                            else
                            {

                                CurrentEditTrace13List.Add(trace13);
                                CurrentTempEditTrace13 = trace13;
                            }
                            #endregion

                            #region 记录序列号
                            if (ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && ChooseGoodsInfo[0].childrentracecode != null && ChooseGoodsInfo[0].childrentracecode.Count > 0)
                            {
                                foreach (var child in ChooseGoodsInfo[0].childrentracecode)
                                {
                                    var trace12 = new GoodsTrace12();
                                    trace12.cbilid = XCSTOCKMainInfo.cbilid;
                                    trace12.id1 = info.id1;
                                    long Iid12 = 0;
                                    if (CurrentEditTrace12List != null && CurrentEditTrace12List.Count > 0)
                                    {
                                        for (int i = 0; i < CurrentEditTrace12List.Count; i++)
                                        {
                                            if (ConvertHelper.ToLong(CurrentEditTrace12List[i].id12) > Iid12)
                                            {
                                                Iid12 = ConvertHelper.ToLong(CurrentEditTrace12List[i].id12);
                                            }
                                        }
                                    }
                                    trace12.id12 = Iid12 + 1;
                                    trace12.cgoodsid = ChooseGoodsInfo[0].cgoodsid;
                                    trace12.cph = (iSamePHProcess > 1 ? info.cph : ChooseGoodsInfo[0].cph);
                                    trace12.dmadedate = (iSamePHProcess > 1 ? info.dmadedate : ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate));
                                    trace12.dexpdate = (iSamePHProcess > 1 ? info.dexpdate : ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate));
                                    trace12.cmjph = "";
                                    trace12.ctracename = child;
                                    trace12.cirter = GlobalProxySetting.GetLoginState().EmployeeCode;
                                    trace12.cparenttrace = ChooseGoodsInfo[0].cudicode;
                                    trace12.cdicode = ChooseGoodsInfo[0].ctracecode;
                                    trace12.fzsmqty = ChooseGoodsInfo[0].fqty;
                                    trace12.cnote = "";//info.cbilid;
                                    trace12.corgid = GlobalProxySetting.GetLoginState().OrgID;
                                    if (CurrentEditTrace12List != null && CurrentEditTrace12List.Any(p => p.cgoodsid == ChooseGoodsInfo[0].cgoodsid && p.cph == trace12.cph && p.ctracename == child && p.cparenttrace == ChooseGoodsInfo[0].cudicode))
                                    {
                                        for (int i = 0; i < CurrentEditTrace12List.Count(); i++)
                                        {
                                            if (CurrentEditTrace12List[i].cgoodsid == ChooseGoodsInfo[0].cgoodsid && CurrentEditTrace12List[i].cph == trace12.cph && CurrentEditTrace12List[i].ctracename == child && CurrentEditTrace12List[i].cparenttrace == ChooseGoodsInfo[0].cudicode)
                                            {
                                                trace12.id1 = CurrentEditTrace12List[i].id1;
                                                trace12.id12 = CurrentEditTrace12List[i].id12;
                                                CurrentEditTrace12List[i] = trace12;
                                                CurrentTempEditTrace12 = trace12;
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {

                                        CurrentEditTrace12List.Add(trace12);
                                        CurrentTempEditTrace12 = trace12;
                                    }
                                }
                            }
                            else
                            {
                                #region 记录序列号
                                if (ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
                                {
                                    var trace12 = new GoodsTrace12();
                                    trace12.cbilid = XCSTOCKMainInfo.cbilid;
                                    trace12.id1 = info.id1;
                                    long Iid12 = 0;
                                    if (CurrentEditTrace12List != null && CurrentEditTrace12List.Count > 0)
                                    {
                                        for (int i = 0; i < CurrentEditTrace12List.Count; i++)
                                        {
                                            if (ConvertHelper.ToLong(CurrentEditTrace12List[i].id12) > Iid12)
                                            {
                                                Iid12 = ConvertHelper.ToLong(CurrentEditTrace12List[i].id12);
                                            }
                                        }
                                    }
                                    trace12.id12 = Iid12 + 1;
                                    trace12.cgoodsid = ChooseGoodsInfo[0].cgoodsid;
                                    trace12.cph = (iSamePHProcess > 1 ? info.cph : ChooseGoodsInfo[0].cph);
                                    trace12.dmadedate = (iSamePHProcess > 1 ? info.dmadedate : ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate));
                                    trace12.dexpdate = (iSamePHProcess > 1 ? info.dexpdate : ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate));
                                    trace12.cmjph = "";
                                    trace12.cirter = GlobalProxySetting.GetLoginState().EmployeeCode;
                                    trace12.cparenttrace = ChooseGoodsInfo[0].cudicode;
                                    trace12.ctracename = "";
                                    trace12.cdicode = ChooseGoodsInfo[0].ctracecode;
                                    var findrow = CurrentEditTrace12List.Where(a => a.id1 == info.id1 && a.ctracename == "" && a.cparenttrace == ChooseGoodsInfo[0].cudicode).FirstOrDefault();
                                    if (findrow != null)
                                    {
                                        trace12.fzsmqty = findrow.fzsmqty + ChooseGoodsInfo[0].fqty;
                                    }
                                    else
                                    {
                                        trace12.fzsmqty = ChooseGoodsInfo[0].fqty;
                                    }
                                    trace12.corgid = GlobalProxySetting.GetLoginState().OrgID;
                                    trace12.cnote = "";// info.cbilid;
                                    CurrentTempEditTrace12 = trace12;
                                    if (CurrentEditTrace12List != null && CurrentEditTrace12List.Any(p => p.cgoodsid == ChooseGoodsInfo[0].cgoodsid && p.cph == trace12.cph && p.cparenttrace == ChooseGoodsInfo[0].cudicode))
                                    {
                                        for (int i = 0; i < CurrentEditTrace12List.Count(); i++)
                                        {
                                            if (CurrentEditTrace12List[i].cgoodsid == ChooseGoodsInfo[0].cgoodsid && CurrentEditTrace12List[i].cph == trace12.cph && CurrentEditTrace12List[i].cparenttrace == ChooseGoodsInfo[0].cudicode)
                                            {
                                                trace12.id1 = CurrentEditTrace12List[i].id1;
                                                trace12.id12 = CurrentEditTrace12List[i].id12;
                                                CurrentEditTrace12List[i] = trace12;
                                                CurrentTempEditTrace12 = trace12;
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {

                                        CurrentEditTrace12List.Add(trace12);
                                        CurrentTempEditTrace12 = trace12;
                                    }
                                }
                                #endregion
                            }
                            #endregion
                        }
                    }
                    else
                    {
                        #region 记录序列号
                        if (ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && !string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].ctracecode))
                        {
                            var trace12 = new GoodsTrace12();
                            trace12.cbilid = XCSTOCKMainInfo.cbilid;
                            trace12.id1 = info.id1;
                            long Iid12 = 0;
                            if (CurrentEditTrace12List != null && CurrentEditTrace12List.Count > 0)
                            {
                                for (int i = 0; i < CurrentEditTrace12List.Count; i++)
                                {
                                    if (ConvertHelper.ToLong(CurrentEditTrace12List[i].id12) > Iid12)
                                    {
                                        Iid12 = ConvertHelper.ToLong(CurrentEditTrace12List[i].id12);
                                    }
                                }
                            }
                            trace12.id12 = Iid12 + 1;
                            trace12.cgoodsid = ChooseGoodsInfo[0].cgoodsid;
                            trace12.cph = (iSamePHProcess > 1 ? info.cph : ChooseGoodsInfo[0].cph);
                            trace12.dmadedate = (iSamePHProcess > 1 ? info.dmadedate : ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate));
                            trace12.dexpdate = (iSamePHProcess > 1 ? info.dexpdate : ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate));
                            trace12.cmjph = "";
                            trace12.cirter = GlobalProxySetting.GetLoginState().EmployeeCode;
                            trace12.cparenttrace = ChooseGoodsInfo[0].cudicode;
                            trace12.ctracename = ChooseGoodsInfo[0].ctracecode;
                            trace12.cdicode = ChooseGoodsInfo[0].ctracecode;
                            var findrow = CurrentEditTrace12List.Where(a => a.id1 == info.id1 && a.ctracename == "" && a.cparenttrace == ChooseGoodsInfo[0].cudicode).FirstOrDefault();
                            if (findrow != null)
                            {
                                trace12.fzsmqty = findrow.fzsmqty + ChooseGoodsInfo[0].fqty;
                            }
                            else
                            {
                                trace12.fzsmqty = ChooseGoodsInfo[0].fqty;
                            }
                            trace12.corgid = GlobalProxySetting.GetLoginState().OrgID;
                            trace12.cnote = "";// info.cbilid;
                            CurrentTempEditTrace12 = trace12;
                            if (CurrentEditTrace12List != null && CurrentEditTrace12List.Any(p => p.cgoodsid == ChooseGoodsInfo[0].cgoodsid && p.cph == trace12.cph && p.ctracename == ChooseGoodsInfo[0].ctracecode && p.cparenttrace == ChooseGoodsInfo[0].cudicode))
                            {
                                for (int i = 0; i < CurrentEditTrace12List.Count(); i++)
                                {
                                    if (CurrentEditTrace12List[i].cgoodsid == ChooseGoodsInfo[0].cgoodsid && CurrentEditTrace12List[i].cph == trace12.cph && CurrentEditTrace12List[i].ctracename == ChooseGoodsInfo[0].ctracecode && CurrentEditTrace12List[i].cparenttrace == ChooseGoodsInfo[0].cudicode)
                                    {
                                        trace12.id1 = CurrentEditTrace12List[i].id1;
                                        trace12.id12 = CurrentEditTrace12List[i].id12;
                                        CurrentEditTrace12List[i] = trace12;
                                        CurrentTempEditTrace12 = trace12;
                                        break;
                                    }
                                }
                            }
                            else
                            {

                                CurrentEditTrace12List.Add(trace12);
                                CurrentTempEditTrace12 = trace12;
                            }
                        }
                        #endregion
                    }
                }
            }
        }

        //V1.3
        private void AddCurrentEditTraceListNewFlow(XCSTOCKGoods info)
        {
            if (bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].cudicode))
                {

                    #region 记录序列号
                    if (ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && ChooseGoodsInfo[0].childrentracecode != null && ChooseGoodsInfo[0].childrentracecode.Count > 0)
                    {
                        foreach (var child in ChooseGoodsInfo[0].childrentracecode)
                        {
                            var trace12 = new GoodsTrace12();
                            trace12.cbilid = (info.cbilid == "Add" ? info.ref_cbilid : info.cbilid); //info.ref_cbilid;
                            trace12.id1 = info.id1;
                            long Iid12 = 0;
                            if (CurrentEditTrace12List != null && CurrentEditTrace12List.Count > 0)
                            {
                                for (int i = 0; i < CurrentEditTrace12List.Count; i++)
                                {
                                    if (ConvertHelper.ToLong(CurrentEditTrace12List[i].id12) > Iid12)
                                    {
                                        Iid12 = ConvertHelper.ToLong(CurrentEditTrace12List[i].id12);
                                    }
                                }
                            }
                            trace12.id12 = Iid12 + 1;
                            trace12.cgoodsid = ChooseGoodsInfo[0].cgoodsid;
                            trace12.cph = (iSamePHProcess > 1 ? info.cph : ChooseGoodsInfo[0].cph);
                            trace12.dmadedate = (iSamePHProcess > 1 ? info.dmadedate : ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate));
                            trace12.dexpdate = (iSamePHProcess > 1 ? info.dexpdate : ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate));
                            trace12.cmjph = "";
                            trace12.ctracename = child;
                            trace12.cdicode = ChooseGoodsInfo[0].ctracecode;
                            trace12.cirter = GlobalProxySetting.GetLoginState().EmployeeCode;
                            trace12.cparenttrace = ChooseGoodsInfo[0].cudicode;
                            trace12.cdicode = ChooseGoodsInfo[0].ctracecode;
                            trace12.fzsmqty = ChooseGoodsInfo[0].fqty;
                            trace12.corgid = GlobalProxySetting.GetLoginState().OrgID;
                            trace12.cnote = info.ref_cbilid;


                            CurrentEditTrace12List.Add(trace12);
                            CurrentTempEditTrace12 = trace12;

                        }
                    }
                    else
                    {
                        #region 记录序列号
                        if (ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
                        {
                            var trace12 = new GoodsTrace12();
                            trace12.cbilid = (info.cbilid == "Add" ? info.ref_cbilid : info.cbilid); // info.ref_cbilid;
                            trace12.id1 = info.id1;
                            long Iid12 = 0;
                            if (CurrentEditTrace12List != null && CurrentEditTrace12List.Count > 0)
                            {
                                for (int i = 0; i < CurrentEditTrace12List.Count; i++)
                                {
                                    if (ConvertHelper.ToLong(CurrentEditTrace12List[i].id12) > Iid12)
                                    {
                                        Iid12 = ConvertHelper.ToLong(CurrentEditTrace12List[i].id12);
                                    }
                                }
                            }
                            trace12.id12 = Iid12 + 1;
                            trace12.cgoodsid = ChooseGoodsInfo[0].cgoodsid;
                            trace12.cph = (iSamePHProcess > 1 ? info.cph : ChooseGoodsInfo[0].cph);
                            trace12.dmadedate = (iSamePHProcess > 1 ? info.dmadedate : ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate));
                            trace12.dexpdate = (iSamePHProcess > 1 ? info.dexpdate : ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate));
                            trace12.cmjph = "";
                            trace12.cirter = GlobalProxySetting.GetLoginState().EmployeeCode;
                            trace12.cparenttrace = ChooseGoodsInfo[0].cudicode;
                            trace12.ctracename = "";
                            trace12.cdicode = ChooseGoodsInfo[0].ctracecode;
                            //var findrow = CurrentEditTrace12List.Where(a => a.id1 == info.id1 && a.ctracename == "" && a.cparenttrace == ChooseGoodsInfo[0].cudicode).FirstOrDefault();
                            trace12.fzsmqty = ChooseGoodsInfo[0].fqty;

                            trace12.corgid = GlobalProxySetting.GetLoginState().OrgID;
                            trace12.cnote = info.ref_cbilid;
                            CurrentTempEditTrace12 = trace12;

                            CurrentEditTrace12List.Add(trace12);
                            CurrentTempEditTrace12 = trace12;

                        }
                        #endregion
                    }
                    #endregion
                }
            }
        }

        /// <summary>
        /// 待复核
        /// </summary>
        private void BtnUncheck_Click(object sender, EventArgs e)
        {
            if (resultList != null && resultList.Count > 0)
            {
                var resultdfhList = resultList.Where(a => a.fnoticeqty > (a.ffhqty + a.fcancelqty)).ToList();

                var dt = DataTableHelper.ToDataTabel<XCSTOCKGoods>(resultdfhList);
                var goodsList = dt.Select().ToList();
                dsdataRows = goodsList;
                drCurr = goodsList.FirstOrDefault();
                var cbilid = (MainGoodsInfo != null) ? MainGoodsInfo.cbilid : "";
                int iposition = -1;

                #region
                //var view = LayoutInflater.Inflate(Resource.Layout.PDAxcstockDFH, null);
                //var layoutID = Resource.Layout.PDAxcstockDFHListItem;
                //var adapter = new ListViewCommonAdapter<DataRow>(this, goodsList, layoutID);
                //var cbilid =(MainGoodsInfo!=null)? MainGoodsInfo.cbilid:""; 
                //adapter.OnGetView += (position, convertView, parent, item, viewHolder) =>
                //{
                //    Action<Color> action = (color) =>
                //    {
                //        viewHolder.GetView<TextView>(Resource.Id.textView1).SetTextColor(color);
                //        viewHolder.GetView<TextView>(Resource.Id.textView2).SetTextColor(color);
                //        viewHolder.GetView<TextView>(Resource.Id.textView3).SetTextColor(color);
                //    };
                //    viewHolder.SetText(Resource.Id.textView1, item["ccommonname"].ToString() );
                //    viewHolder.SetText(Resource.Id.textView2, item["cph"].ToString());
                //    viewHolder.SetText(Resource.Id.textView3, item["fqty"].ToString());

                //    if (drCurr["id1"].ToString() == item["id1"].ToString())
                //    {
                //        //viewHolder.GetConvertView().SetBackgroundResource(Resource.Drawable.rectangle);
                //        //viewHolder.GetConvertView().SetBackgroundColor(custom_blue);
                //    }
                //    else
                //    {
                //        //viewHolder.GetConvertView().SetBackgroundColor(custom_white);
                //    }
                //    return viewHolder.GetConvertView();
                //};
                //var lvMain = view.FindViewById<ListView>(Resource.Id.listView1);
                //lvMain.Adapter = adapter;
                //var lcbilid= view.FindViewById<TextView>(Resource.Id.txtcbilid);
                //lcbilid.Text = cbilid;//显示单号
                //lvMain.ItemsCanFocus = true;
                //int iposition = -1;
                //lvMain.ItemClick += (a, b) =>
                //{
                //    var tagValue = b.View.Tag as ListViewCommonAdapterViewHolder;
                //    iposition = tagValue.GetPosition();
                //    drCurr = dsdataRows[iposition];
                //    (lvMain.Adapter as ListViewCommonAdapter<DataRow>).NotifyDataSetChanged();
                //};
                //  var alertDialog = new AlertDialog.Builder(this).SetTitle("待扫复核查询").SetView(view)
                //  .SetNeutralButton("取消", new EventHandler<DialogClickEventArgs>((aaa, bbb) => {

                //  }
                //))
                // .SetPositiveButton("确定", new EventHandler<DialogClickEventArgs>((aaa, bbb) => {
                //if (iposition > -1)
                //{
                //    var cgoodsid = dsdataRows[iposition]["cgoodsid"].ToString();
                //    if (!string.IsNullOrEmpty(cgoodsid))//
                //    {
                //        for (int i = 0; i < resultList.Count; i++)
                //        {
                //            if (resultList[i].id1 == ConvertHelper.ToLong(dsdataRows[iposition]["id1"]))
                //            {
                //                SetFormText(resultList[i]);
                //                GlobalDataCache.SetData("PDACKFHDGoodsInfo", resultList[i]);
                //                break;
                //            }
                //        }

                //    }
                //}
                // }
                //))
                //  .Create();
                //  alertDialog.SetView(view, 0, 0, 0, 0);
                //  alertDialog.Show();
                #endregion

                var builder = new Dialog(new ContextThemeWrapper(this, Resource.Style.CustomAlertDialogStyle));
                LayoutInflater layoutInflater = LayoutInflater.From(this);
                var alertview = layoutInflater.Inflate(Resource.Layout.xcstockdfh, null, false);
                builder.SetContentView(alertview);
                builder.Window.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
                builder.SetCancelable(false);
                var txtcbilid = alertview.FindViewById<TextView>(Resource.Id.txtcbilid);
                txtcbilid.Text = cbilid;
                var adapter = new ListViewCommonAdapter<DataRow>(this, goodsList, Resource.Layout.PDAxcstockDFHListItem);
                adapter.OnGetView += (position, convertView, parent, item, viewHolder) =>
                {
                    Action<Color> action = (color) =>
                    {
                        viewHolder.GetView<TextView>(Resource.Id.textView1).SetTextColor(color);
                        viewHolder.GetView<TextView>(Resource.Id.textView2).SetTextColor(color);
                        viewHolder.GetView<TextView>(Resource.Id.textView3).SetTextColor(color);
                    };
                    viewHolder.SetText(Resource.Id.textView1, item["ccommonname"].ToString());
                    viewHolder.SetText(Resource.Id.textView2, item["cph"].ToString());
                    viewHolder.SetText(Resource.Id.textView3, ConvertHelper.ToDecimal(item["fqty"]).ToString(GlobalDataCache.QtyFormat));

                    return viewHolder.GetConvertView();
                };
                var lvMain = alertview.FindViewById<ListView>(Resource.Id.listView1);
                lvMain.Adapter = adapter;
                lvMain.ItemClick += (a, b) =>
                {
                    var tagValue = b.View.Tag as ListViewCommonAdapterViewHolder;
                    iposition = tagValue.GetPosition();
                    drCurr = dsdataRows[iposition];
                };
                var helper = new ListViewColorHelper<ListViewCommonAdapter<DataRow>>(lvMain);
                alertview.FindViewById(Resource.Id.btn_ok).Click += (s, ev) =>
                {
                    if (iposition > -1)
                    {
                        var cgoodsid = dsdataRows[iposition]["cgoodsid"].ToString();
                        if (!string.IsNullOrEmpty(cgoodsid))//
                        {
                            for (int i = 0; i < resultList.Count; i++)
                            {
                                if (resultList[i].id1 == ConvertHelper.ToLong(dsdataRows[iposition]["id1"]))
                                {
                                    SetFormText(resultList[i]);
                                    GlobalDataCache.SetData("PDACKFHDGoodsInfo", resultList[i]);
                                    break;
                                }
                            }

                        }
                    }
                    builder.Dismiss();
                };
                alertview.FindViewById(Resource.Id.btn_cancel).Click += (s, ev) =>
                {
                    builder.Dismiss();
                };
                builder.Show();
            }
        }
        /// <summary>
        /// 清空复核数据
        /// </summary>
        private void btnclearcheck_Click(object sender, EventArgs e)
        {
            CustomAlertDialog custom = new CustomAlertDialog();
            custom.OKClick += (s1, e1) =>
            {
                //处理清空复核逻辑
                ClearFormText("clear");
            };
            custom.AlertDialogShow(this, "是否确认清空复核数据？");

        }

        /// <summary>
        /// 出库复核完成
        /// </summary>
        private void BtnEnd_Click(object sender, EventArgs e)
        {
            if (resultList == null || resultList.Count <= 0)
            {
                Toast.MakeText(this, "出库复核明细为空，不允许过账！", ToastLength.Short).Show();
                return;
            }
            CustomAlertDialog dialog = new CustomAlertDialog();
            dialog.OKClick += (s1, e1) =>
            {
                this.Proxy.Execute(new PDAXCSTOCKOPRequest()
                {
                    OrgID = GlobalProxySetting.OrgID,
                    EmpCode = GlobalProxySetting.GetLoginState().EmployeeCode,
                    OPType = 5,
                    cbilid = XCSTOCKMainInfo.cbilid,
                    LRInfoList = resultList
                },
            (response) =>
            {
                if (!response.IsError)
                {
                    Toast.MakeText(this, "出库复核完成！", ToastLength.Short).Show();
                    TB_NavigationOnClick(null, null);
                }
            },
            this);
            };
            var showText = "是否过账出库复核单！";
            dialog.AlertDialogShow(this, showText);
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Back && e.Action == KeyEventActions.Down)
            {
                CustomAlertDialog custom = new CustomAlertDialog();
                custom.OKClick += (s1, e1) =>
                {
                    TB_NavigationOnClick(null, null);
                };
                custom.AlertDialogShow(this, "确定返回出库复核列表？");
                return true;
            }
            if (keyCode == Keycode.VolumeDown) //手机声音加键触发
            {
                txtInput.Text = "";
                txtInput.Focusable = true;
                txtInput.FocusableInTouchMode = true;
                txtInput.RequestFocus();
                scanner = new MobileBarcodeScanner();
                Task t = new Task(AutoScan);
                t.Start();
                return true;
            }


            if (keyCode == Keycode.VolumeUp) //手机声音减键触发
            {
                txtInput.Text = "";
                txtInput.Focusable = true;
                txtInput.FocusableInTouchMode = true;
                txtInput.RequestFocus();
                scanner = new MobileBarcodeScanner();
                Task t = new Task(AutoScan);
                t.Start();
                return true;
            }
            return base.OnKeyDown(keyCode, e);
        }
        private bool DealSCanRQCode()
        {
            bool bResult = false;
            var txt = txtInput.Text.Trim();
            List<QRDealGoodsData> infoQrList = new List<QRDealGoodsData>();
            List<QRDealGoodsData> infoQrList2 = new List<QRDealGoodsData>();
            dialogQRList = new List<DataRow>();
            infoQr = new QRDealGoodsData();
            var info = new QRDealGoodsData();
            DataTable dt = new DataTable();
            txt = DataTableHelper.DealSCanQRCode(txt, ref infoQrList, ref dt);
            if (infoQrList.Count > 0 && dt.Rows.Count > 0)
            {
                if (dt.Rows.Count > 1)//需要弹框选择规则
                {
                    foreach (DataRow row1 in dt.Rows)
                    {
                        for (int i = 0; i < infoQrList.Count; i++)
                        {
                            if (ConvertHelper.ToString(infoQrList[i].cgoodsid) == ConvertHelper.ToString(row1["cgoodsid"]) || (dt.Columns.Contains("cbarcode") && ConvertHelper.ToString(infoQrList[i].cgoodsid) == ConvertHelper.ToString(row1["cbarcode"])))
                            {
                                info = new QRDealGoodsData();
                                info.cgoodsid = infoQrList[i].cgoodsid;
                                info.cpkname = infoQrList[i].cpkname;
                                info.cph = infoQrList[i].cph;
                                info.dmadedate = infoQrList[i].dmadedate;
                                info.dexpdate = infoQrList[i].dexpdate;
                                infoQrList2.Add(info);
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < infoQrList.Count; i++)
                    {
                        if (ConvertHelper.ToString(infoQrList[i].cgoodsid) == ConvertHelper.ToString(dt.Rows[0]["cgoodsid"]) || (dt.Columns.Contains("cbarcode") && ConvertHelper.ToString(infoQrList[i].cgoodsid) == ConvertHelper.ToString(dt.Rows[0]["cbarcode"])))//查到一个药品信息
                        {
                            info = new QRDealGoodsData();
                            info.cgoodsid = infoQrList[i].cgoodsid;
                            info.cpkname = infoQrList[i].cpkname;
                            info.cph = infoQrList[i].cph;
                            info.dmadedate = infoQrList[i].dmadedate;
                            info.dexpdate = infoQrList[i].dexpdate;
                            infoQrList2.Add(info);
                        }
                    }
                }

                dt = DataTableHelper.ToDataTabel<QRDealGoodsData>(infoQrList2);
                var listrq = dt.Select().ToList();
                if (infoQrList2.Count() > 1)
                {
                    dialogQRList = listrq;
                    popUDIWindowAdapter = new CommonPopwindowAdapter<DataRow>(this, dialogQRList, this, Resource.Layout.RrCodeChose);
                    popUDIWindowAdapter.SetListViewColumn += (position, convertView, parent, row, viewHolder) =>
                    {
                        viewHolder.SetText(Resource.Id.textView1, row["cgoodsname"].ToString() + "\\" + row["cgoodsid"].ToString());//+ "\\"+row["cgoodsid"].ToString()
                        viewHolder.SetText(Resource.Id.textView2, row["cpkname"].ToString());
                        viewHolder.SetText(Resource.Id.textView3, row["cph"].ToString());
                        viewHolder.SetText(Resource.Id.textView4, row["dexpdate"].ToString());
                        return viewHolder.GetConvertView();
                    };
                    popUDIWindowAdapter.SetOnDismissListener(this);
                    //popWindowAdapter.Width = txtcBarcode.Width;
                    popUDIWindowAdapter.ShowAsDropDown(txtInput);
                    bResult = true;
                }
                else
                {
                    infoQr = info;
                    txtInput.Text = infoQr.cgoodsid;
                    bResult = true;
                }
            }
            return bResult;
        }
        private async Task<bool> DealUDICodeAsync()
        {
            var txt = txtInput.Text.Trim();
            bool bResult = false;
            if (string.IsNullOrWhiteSpace(txt))
            {
                return bResult;
            }
            if (bchkUDI)    //先解析组合条码
            {
                var _ErrMsg = "";
                ChooseGoodsInfo = new List<InstrumentGoodsInfo>();
                var UDIGoodsInfo = new List<InstrumentGoodsInfo>();

                ////var request = new TranslateInstrumentCodeRequest()
                ////{
                ////    Code = txt,
                ////    IsCheckUDIAssistcode = true
                ////};

                ////var response = this.Proxy.Execute(request);
                //if (!string.IsNullOrWhiteSpace(_lastCode))
                //{
                //    txt = _lastCode + txt;
                //}
                var d1 = new List<Dictionary<string, string>>();
                foreach (var item in resultList)
                {
                    var dic = new Dictionary<string, string>();
                    //case 0: return ConvertHelper.ToString(row["cgoodsid"]);
                    //case 1: return ConvertHelper.ToString(row["cgoodsid_v_cgoodsname"]);
                    dic["cgoodsid"] = item.cgoodsid;
                    dic["cgoodsid_v_cgoodsname"] = item.cgoodsname;
                    d1.Add(dic);
                }
                //this.RunOnUiThread(() =>
                //{
                //UDIGoodsInfo, UDIlastCode, UDIastUDIRuleLen, UDIRulecode, _ErrMsg)
                //var (_udiGoodsInfo, _uDIlastCode, _uDIastUDIRuleLen, _UDIRulecode, ErrMsg)
                (UDIGoodsInfo, UDIlastCode, UDIastUDIRuleLen, UDIRulecode, _ErrMsg) = await DataTableHelper.GetUdiInstorCodeExAsync(txt, _Rulecode, _LastUDIgoodsid, UDIlastCode, UDIastUDIRuleLen, UDIRulecode, _ErrMsg, fz_chk_sel.Checked, d1, this);
                //});
                if (!string.IsNullOrWhiteSpace(_ErrMsg))
                {
                    Toast.MakeText(this, _ErrMsg, ToastLength.Long).Show();
                    isPopuChooseForm = false;
                    this.txtInput.Text = "";
                    return bResult;
                }
                if (UDIGoodsInfo != null && UDIGoodsInfo.Count > 0)
                {
                    if (UDIGoodsInfo.Count > 1)//多个弹框选择
                    {
                        var udidt = DataTableHelper.ToDataTabel(UDIGoodsInfo);

                        dialogUDIList = udidt.Select("").ToList();
                        popUDIWindowAdapter = new CommonPopwindowAdapter<DataRow>(this, dialogUDIList, this, Resource.Layout.UDIListDetailPopWindow);
                        popUDIWindowAdapter.SetListViewColumn += (position, convertView, parent, row, viewHolder) =>
                        {
                            viewHolder.SetText(Resource.Id.textView1, row["cgoodsname"].ToString() + "\\" + row["cgoodsid"].ToString());//+ "\\"+row["cgoodsid"].ToString()
                            viewHolder.SetText(Resource.Id.textView2, row["cpkname"].ToString());
                            viewHolder.SetText(Resource.Id.textView3, row["cph"].ToString());
                            viewHolder.SetText(Resource.Id.textView4, row["ctracecode"].ToString());
                            viewHolder.SetText(Resource.Id.textView5, row["crulecode"].ToString());
                            return viewHolder.GetConvertView();
                        };
                        popUDIWindowAdapter.SetOnDismissListener(this);
                        //popWindowAdapter.Width = txtcBarcode.Width;
                        popUDIWindowAdapter.ShowAsDropDown(txtInput);
                        bResult = true;
                    }
                    else
                    {
                        ChooseGoodsInfo = UDIGoodsInfo;
                        _Rulecode = ChooseGoodsInfo[0].crulecode;
                        _LastUDIgoodsid = ChooseGoodsInfo[0].cgoodsid;
                        bResult = true;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(UDIlastCode))
                {
                    bResult = true;
                    this.txtInput.Text = "";
                    _lastCode = UDIlastCode;
                    return bResult;
                }
                else
                {
                    Toast.MakeText(this, "未解析出条码规则或条码规则解析错误！", ToastLength.Long).Show();
                    isPopuChooseForm = false;
                    this.txtInput.Text = "";
                }
                if ((!string.IsNullOrWhiteSpace(_lastCode)) && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && UDIastUDIRuleLen == ChooseGoodsInfo[0].icodelen) //辅助条码组合后清空变量
                {
                    _lastCode = "";
                }
                else
                {
                    _lastCode = UDIlastCode;
                }
                _lastUDIRuleLen = UDIastUDIRuleLen;
                _Rulecode = UDIRulecode;

            }
            return bResult;
        }
        private async Task TxtInput_KeyPressAsync(object sender, View.KeyEventArgs e)
        {
            e.Handled = false;
            if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Up)
            {
                #region 组合条码
                if (bchkUDI)
                {
                    if (await DealUDICodeAsync())
                    {
                        var currgoods = GlobalDataCache.GetData<XCSTOCKGoods>("PDACKFHDGoodsInfo");

                        if (!string.IsNullOrWhiteSpace(_lastCode))
                        {
                            this.txtInput.RequestFocus();
                            e.Handled = true;
                            return;
                        }
                        else if (currgoods != null && CurrentEditTrace12List != null && CurrentEditTrace12List.Count > 0 && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && CurrentEditTrace12List.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph.Contains(ChooseGoodsInfo[0].cph)).Any() && currgoods.cgoodsid == ChooseGoodsInfo[0].cgoodsid)
                        {
                            var tmpcph = CurrentEditTrace12List.Where(a => a.cgoodsid == currgoods.cgoodsid && a.cnote == currgoods.cbilid && a.cph == ChooseGoodsInfo[0].cph && a.id1 == currgoods.id1).FirstOrDefault();
                            if (iSamePHProcess > 1)
                            {
                                tmpcph = CurrentEditTrace12List.Where(a => a.cgoodsid == currgoods.cgoodsid && a.cnote == currgoods.cbilid && a.cph.Contains(ChooseGoodsInfo[0].cph) && a.dmadedate == currgoods.dmadedate && a.dexpdate == currgoods.dexpdate && a.cparenttrace == ChooseGoodsInfo[0].cudicode).FirstOrDefault();
                            }
                            if (tmpcph != null)
                            {
                                //更新数量 
                                var bresult = false;
                                var tmp = CoumTracefqty(currgoods, "check", ref bresult);
                                if (!bresult)
                                {
                                    if (tmp + ConvertHelper.ToDecimal(currgoods.finputqty) > currgoods.fnoticeqty)
                                    {
                                        Toast.MakeText(this, "出库复核数量[" + tmp.ToString() + "]+复核异常数量[" + ConvertHelper.ToDecimal(currgoods.finputqty).ToString() + "]不能大于通知数量[" + currgoods.fnoticeqty.ToString() + "]！", 0).Show();
                                        this.txtInput.Text = "";
                                        e.Handled = true;
                                    }
                                    else
                                    {
                                        //this.txtfqty.Text = tmp.ToString(GlobalDataCache.QtyFormat);
                                        //this.txtvalue.Text = (ConvertHelper.ToDecimal(txtfqty.Text) * ConvertHelper.ToDecimal(txtprice.Text)).ToString(GlobalDataCache.ValueFormat);
                                        currgoods.ffhqty = tmp;
                                        AddCurrentEditTraceList(ref currgoods);
                                        GlobalDataCache.SetData("PDACKFHDGoodsInfo", currgoods);
                                        for (int i = 0; i < resultList.Count; i++)
                                        {
                                            if (resultList[i].id1 == currgoods.id1)
                                            {
                                                resultList[i].ffhqty = tmp;
                                                break;
                                            }
                                        }
                                    }
                                    _originalAdapter.UpdateData(resultList);
                                    //Txtfqty_KeyPress(sender, e);
                                    //if (resultList != null)
                                    //{
                                    //    listview.Adapter = new MDSHGoodsInfoAdapter(this, resultList.OrderBy(b => b.iflag).ToList());
                                    //}
                                }
                                this.txtInput.Text = "";
                                e.Handled = true;
                                return;
                            }
                            else if (tmpcph == null && CurrentEditTrace13List != null && CurrentEditTrace13List.Count > 0)
                            {
                                var tmpcph13 = CurrentEditTrace13List.Where(a => a.cgoodsid == currgoods.cgoodsid && a.cnote == currgoods.cbilid && a.cph.Contains(ChooseGoodsInfo[0].cph) && a.id1 == currgoods.id1).FirstOrDefault();
                                if (iSamePHProcess > 1)
                                {
                                    tmpcph13 = CurrentEditTrace13List.Where(a => a.cgoodsid == currgoods.cgoodsid && a.cnote == currgoods.cbilid && a.cph.Contains(ChooseGoodsInfo[0].cph) && a.dmadedate == currgoods.dmadedate && a.dexpdate == currgoods.dexpdate && a.ctracename == ChooseGoodsInfo[0].cudicode).FirstOrDefault();
                                }
                                if (GlobalDataCache.p_SOFTUSERTYPE == "zz579" && strcbiltype == "CC" && bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && currgoods != null && ConvertHelper.ToString(currgoods.creserve1) != "")//   KB023 2024-03-28 
                                {
                                    tmpcph13 = CurrentEditTrace13List.Where(a => a.cgoodsid == currgoods.cgoodsid && a.cnote == currgoods.cbilid && a.cph.Contains(ChooseGoodsInfo[0].cph) && a.ctracename == ChooseGoodsInfo[0].cudicode).FirstOrDefault();
                                }
                                if (tmpcph13 != null)
                                {
                                    //更新数量 
                                    var bresult = false;
                                    var tmp = CoumTracefqty(currgoods, "check", ref bresult);
                                    if (!bresult)
                                    {
                                        if (tmp + ConvertHelper.ToDecimal(currgoods.finputqty) > currgoods.fnoticeqty)
                                        {
                                            Toast.MakeText(this, "出库复核数量[" + tmp.ToString() + "]+复核异常数量[" + ConvertHelper.ToDecimal(currgoods.finputqty).ToString() + "]不能大于通知数量[" + currgoods.fnoticeqty.ToString() + "]！", 0).Show();
                                            this.txtInput.Text = "";
                                            e.Handled = true;
                                        }
                                        else
                                        {
                                            //this.txtfqty.Text = tmp.ToString(GlobalDataCache.QtyFormat);
                                            //this.txtvalue.Text = (ConvertHelper.ToDecimal(txtfqty.Text) * ConvertHelper.ToDecimal(txtprice.Text)).ToString(GlobalDataCache.ValueFormat);
                                            currgoods.ffhqty = tmp;
                                            //currgoods.fnormvalue = ConvertHelper.ToDecimal(this.txtvalue.Text);
                                            AddCurrentEditTraceList(ref currgoods);

                                            GlobalDataCache.SetData("PDACKFHDGoodsInfo", currgoods);
                                            //Txtfqty_KeyPress(sender, e);
                                            //if (resultList != null)
                                            //{
                                            //    listview.Adapter = new MDSHGoodsInfoAdapter(this, resultList.OrderBy(b => b.iflag).ToList());
                                            //}
                                            for (int i = 0; i < resultList.Count; i++)
                                            {
                                                if (resultList[i].id1 == currgoods.id1)
                                                {
                                                    resultList[i].ffhqty = tmp;
                                                    break;
                                                }
                                            }
                                        }
                                        _originalAdapter.UpdateData(resultList);
                                    }
                                    this.txtInput.Text = "";
                                    e.Handled = true;
                                    return;
                                }
                                else
                                {
                                    this.txtInput.Text = ChooseGoodsInfo[0].cgoodsid;
                                }
                            }
                            else
                            {
                                this.txtInput.Text = ChooseGoodsInfo[0].cgoodsid;
                            }
                        }
                        else if (currgoods != null && CurrentEditTrace13List != null && CurrentEditTrace13List.Count > 0 && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && CurrentEditTrace13List.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph.Contains(ChooseGoodsInfo[0].cph)).Any() && currgoods.cgoodsid == ChooseGoodsInfo[0].cgoodsid)
                        {
                            var tmpcph = CurrentEditTrace13List.Where(a => a.cgoodsid == currgoods.cgoodsid && a.cnote == currgoods.cbilid && a.cph == ChooseGoodsInfo[0].cph && a.id1 == currgoods.id1).FirstOrDefault();
                            if (iSamePHProcess > 1)
                            {
                                tmpcph = CurrentEditTrace13List.Where(a => a.cgoodsid == currgoods.cgoodsid && a.cnote == currgoods.cbilid && a.cph.Contains(ChooseGoodsInfo[0].cph) && a.dmadedate == currgoods.dmadedate && a.dexpdate == currgoods.dexpdate && a.ctracename == ChooseGoodsInfo[0].cudicode).FirstOrDefault();
                            }
                            if (GlobalDataCache.p_SOFTUSERTYPE == "zz579" && strcbiltype == "CC" && bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && currgoods != null && ConvertHelper.ToString(currgoods.creserve1) != "")//   KB023 2024-03-28 
                            {
                                tmpcph = CurrentEditTrace13List.Where(a => a.cgoodsid == currgoods.cgoodsid && a.cnote == currgoods.cbilid && a.cph.Contains(ChooseGoodsInfo[0].cph) && a.ctracename == ChooseGoodsInfo[0].cudicode).FirstOrDefault();
                            }
                            if (tmpcph != null)
                            {
                                //更新数量 
                                var bresult = false;
                                var tmp = CoumTracefqty(currgoods, "check", ref bresult);
                                if (!bresult)
                                {
                                    if (tmp + ConvertHelper.ToDecimal(currgoods.finputqty) > currgoods.fnoticeqty)
                                    {
                                        Toast.MakeText(this, "出库复核数量[" + tmp.ToString() + "]+复核异常数量[" + ConvertHelper.ToDecimal(currgoods.finputqty).ToString() + "]不能大于通知数量[" + currgoods.fnoticeqty.ToString() + "]！", 0).Show();
                                        this.txtInput.Text = "";
                                        e.Handled = true;
                                    }
                                    else
                                    {
                                        //this.txtfqty.Text = tmp.ToString(GlobalDataCache.QtyFormat);
                                        //this.txtvalue.Text = (ConvertHelper.ToDecimal(txtfqty.Text) * ConvertHelper.ToDecimal(txtprice.Text)).ToString(GlobalDataCache.ValueFormat);
                                        currgoods.ffhqty = tmp;
                                        AddCurrentEditTraceList(ref currgoods);

                                        GlobalDataCache.SetData("PDACKFHDGoodsInfo", currgoods);
                                        //Txtfqty_KeyPress(sender, e);
                                        //if (resultList != null)
                                        //{
                                        //    listview.Adapter = new MDSHGoodsInfoAdapter(this, resultList.OrderBy(b => b.iflag).ToList());
                                        //}
                                        for (int i = 0; i < resultList.Count; i++)
                                        {
                                            if (resultList[i].id1 == currgoods.id1)
                                            {
                                                resultList[i].ffhqty = tmp;
                                                break;
                                            }
                                        }
                                    }
                                    _originalAdapter.UpdateData(resultList);
                                }
                                this.txtInput.Text = "";
                                e.Handled = true;
                                return;
                            }
                            else if (tmpcph == null && CurrentEditTrace12List != null && CurrentEditTrace12List.Count > 0)
                            {
                                var tmpcph12 = CurrentEditTrace12List.Where(a => a.cgoodsid == currgoods.cgoodsid && a.cnote == currgoods.cbilid && a.cph.Contains(ChooseGoodsInfo[0].cph) && a.id1 == currgoods.id1).FirstOrDefault();
                                if (iSamePHProcess > 1)
                                {
                                    tmpcph12 = CurrentEditTrace12List.Where(a => a.cgoodsid == currgoods.cgoodsid && a.cnote == currgoods.cbilid && a.cph.Contains(ChooseGoodsInfo[0].cph) && a.dmadedate == tmpcph.dmadedate && a.dexpdate == tmpcph.dexpdate && a.cparenttrace == ChooseGoodsInfo[0].cudicode).FirstOrDefault();
                                }
                                if (tmpcph12 != null)
                                {
                                    //更新数量 
                                    var bresult = false;
                                    var tmp = CoumTracefqty(currgoods, "check", ref bresult);
                                    if (!bresult)
                                    {
                                        if (tmp + ConvertHelper.ToDecimal(currgoods.finputqty) > currgoods.fnoticeqty)
                                        {
                                            Toast.MakeText(this, "出库复核数量[" + tmp.ToString() + "]+复核异常数量[" + ConvertHelper.ToDecimal(currgoods.finputqty).ToString() + "]不能大于通知数量[" + currgoods.fnoticeqty.ToString() + "]！", 0).Show();
                                            this.txtInput.Text = "";
                                            e.Handled = true;
                                        }
                                        else
                                        {
                                            //this.txtfqty.Text = tmp.ToString(GlobalDataCache.QtyFormat);
                                            //this.txtvalue.Text = (ConvertHelper.ToDecimal(txtfqty.Text) * ConvertHelper.ToDecimal(txtprice.Text)).ToString(GlobalDataCache.ValueFormat);
                                            currgoods.ffhqty = tmp;
                                            AddCurrentEditTraceList(ref currgoods);
                                            GlobalDataCache.SetData("PDACKFHDGoodsInfo", currgoods);
                                            //Txtfqty_KeyPress(sender, e);
                                            //if (resultList != null)
                                            //{
                                            //    listview.Adapter = new MDSHGoodsInfoAdapter(this, resultList.OrderBy(b => b.iflag).ToList());
                                            //}
                                            for (int i = 0; i < resultList.Count; i++)
                                            {
                                                if (resultList[i].id1 == currgoods.id1)
                                                {
                                                    resultList[i].ffhqty = tmp;
                                                    break;
                                                }
                                            }
                                        }
                                        _originalAdapter.UpdateData(resultList);
                                    }
                                    this.txtInput.Text = "";
                                    e.Handled = true;
                                    return;
                                }
                                else
                                {
                                    this.txtInput.Text = ChooseGoodsInfo[0].cgoodsid;
                                }
                            }
                            else
                            {
                                this.txtInput.Text = ChooseGoodsInfo[0].cgoodsid;
                            }
                        }
                        else if (ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && iSamePHProcess > 1 && currgoods != null && ChooseGoodsInfo[0].cgoodsid == currgoods.cgoodsid && currgoods.cph.Contains(ChooseGoodsInfo[0].cph) && currgoods.dmadedate == ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate) && currgoods.dexpdate == ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate))
                        {
                            var bresult = false;
                            var tmp = CoumTracefqty(currgoods, "check", ref bresult);
                            if (!bresult)
                            {
                                if (tmp + ConvertHelper.ToDecimal(currgoods.finputqty) > currgoods.fnoticeqty)
                                {
                                    Toast.MakeText(this, "出库复核数量[" + tmp.ToString() + "]+复核异常数量[" + ConvertHelper.ToDecimal(currgoods.finputqty).ToString() + "]不能大于通知数量[" + currgoods.fnoticeqty.ToString() + "]！", 0).Show();
                                    this.txtInput.Text = "";
                                    e.Handled = true;
                                }
                                else
                                {
                                    currgoods.ffhqty = tmp;
                                    //扫描的是组合条码，并且品种批号与当前编辑的一致时，直接更新数量,当前数量加1
                                    if (FindViewById<TextView>(Resource.Id.txtcgoodsname).Text != currgoods.cgoodsname && !currgoods.cph.Contains(FindViewById<TextView>(Resource.Id.txtcph).Text))
                                    {
                                        SetNewFormText(currgoods);
                                    }
                                    //if (currgoods.ffhqty + ConvertHelper.ToDecimal(currgoods.finputqty) + ChooseGoodsInfo[0].fqty > currgoods.fnoticeqty)
                                    //{
                                    //    Toast.MakeText(this, "出库复核数量[" + (currgoods.ffhqty + ChooseGoodsInfo[0].fqty).ToString() + "]+复核异常数量[" + ConvertHelper.ToDecimal(currgoods.finputqty).ToString() + "]不能大于通知数量[" + currgoods.fnoticeqty.ToString() + "]！", 0).Show();
                                    //    this.txtInput.Text = "";
                                    //    e.Handled = true;
                                    //}
                                    //else
                                    //{
                                    //this.txtfqty.Text = (ConvertHelper.ToDecimal(txtfqty.Text) + ChooseGoodsInfo[0].fqty).ToString(GlobalDataCache.QtyFormat);
                                    //this.txtvalue.Text = (ConvertHelper.ToDecimal(txtfqty.Text) * ConvertHelper.ToDecimal(txtprice.Text)).ToString(GlobalDataCache.ValueFormat);
                                    if (GlobalDataCache.p_SOFTUSERTYPE == "zz579" && strcbiltype == "CC" && bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
                                    {
                                        var tmpresult2 = resultList.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph.Contains(ChooseGoodsInfo[0].cph) && a.dmadedate == ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate) && a.dexpdate == ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate) && a.creserve1 != "" && a.creserve1 == ChooseGoodsInfo[0].cudicode && a.id1 != 0).FirstOrDefault();
                                        if (tmpresult2 != null && currgoods.creserve1 != ChooseGoodsInfo[0].cudicode)//扫描的udi与当前变量不相等时
                                        {
                                            currgoods = tmpresult2;
                                            GlobalDataCache.SetData("PDACKFHDGoodsInfo", currgoods);
                                        }
                                    }
                                    //currgoods.ffhqty = ConvertHelper.ToDecimal(currgoods.ffhqty + ChooseGoodsInfo[0].fqty);
                                    //currgoods.fnormvalue = ConvertHelper.ToDecimal(this.txtvalue.Text);
                                    AddCurrentEditTraceList(ref currgoods);

                                    GlobalDataCache.SetData("PDACKFHDGoodsInfo", currgoods);
                                    //Txtfqty_KeyPress(sender, e);
                                    for (int i = 0; i < resultList.Count; i++)
                                    {
                                        if (resultList[i].id1 == currgoods.id1)
                                        {
                                            resultList[i].ffhqty = currgoods.ffhqty;
                                            break;
                                        }
                                    }
                                    //}
                                }
                            }
                            _originalAdapter.UpdateData(resultList);
                            this.txtInput.Text = "";
                            e.Handled = true;
                            return;
                        }
                        else if (ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && iSamePHProcess <= 1 && currgoods != null && ChooseGoodsInfo[0].cgoodsid == currgoods.cgoodsid && ChooseGoodsInfo[0].cph == currgoods.cph)
                        {

                            var bresult = false;
                            var tmp = CoumTracefqty(currgoods, "check", ref bresult);
                            if (!bresult)
                            {
                                if (tmp + ConvertHelper.ToDecimal(currgoods.finputqty) > currgoods.fnoticeqty)
                                {
                                    Toast.MakeText(this, "出库复核数量[" + tmp.ToString() + "]+复核异常数量[" + ConvertHelper.ToDecimal(currgoods.finputqty).ToString() + "]不能大于通知数量[" + currgoods.fnoticeqty.ToString() + "]！", 0).Show();
                                    this.txtInput.Text = "";
                                    e.Handled = true;
                                }
                                else
                                {
                                    currgoods.ffhqty = tmp;
                                    //扫描的是组合条码，并且品种批号与当前编辑的一致时，直接更新数量,当前数量加1
                                    if (FindViewById<TextView>(Resource.Id.txtcgoodsname).Text != currgoods.cgoodsname && FindViewById<TextView>(Resource.Id.txtcph).Text != currgoods.cph)
                                    {
                                        SetNewFormText(currgoods);
                                    }
                                    //if (currgoods.ffhqty + ConvertHelper.ToDecimal(currgoods.finputqty) + ChooseGoodsInfo[0].fqty > currgoods.fnoticeqty)
                                    //{
                                    //    Toast.MakeText(this, "出库复核数量[" + (currgoods.ffhqty + ChooseGoodsInfo[0].fqty).ToString() + "]+复核异常数量[" + ConvertHelper.ToDecimal(currgoods.finputqty).ToString() + "]不能大于通知数量[" + currgoods.fnoticeqty.ToString() + "]！", 0).Show();
                                    //    this.txtInput.Text = "";
                                    //    e.Handled = true;
                                    //}
                                    //else
                                    //{
                                    //this.txtfqty.Text = (ConvertHelper.ToDecimal(txtfqty.Text) + ChooseGoodsInfo[0].fqty).ToString(GlobalDataCache.QtyFormat);
                                    //this.txtvalue.Text = (ConvertHelper.ToDecimal(txtfqty.Text) * ConvertHelper.ToDecimal(txtprice.Text)).ToString(GlobalDataCache.ValueFormat);
                                    if (GlobalDataCache.p_SOFTUSERTYPE == "zz579" && strcbiltype == "CC" && bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
                                    {
                                        var tmpresult2 = resultList.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph == ChooseGoodsInfo[0].cph && a.creserve1 != "" && a.creserve1 == ChooseGoodsInfo[0].cudicode && a.id1 != 0).FirstOrDefault();
                                        if (tmpresult2 != null && currgoods.creserve1 != ChooseGoodsInfo[0].cudicode)//扫描的udi与当前变量不相等时
                                        {
                                            currgoods = tmpresult2;
                                            GlobalDataCache.SetData("PDACKFHDGoodsInfo", currgoods);
                                        }
                                    }
                                    //currgoods.ffhqty = ConvertHelper.ToDecimal(currgoods.ffhqty + ChooseGoodsInfo[0].fqty);
                                    //currgoods.fnormvalue = ConvertHelper.ToDecimal(this.txtvalue.Text);
                                    AddCurrentEditTraceList(ref currgoods);

                                    GlobalDataCache.SetData("PDACKFHDGoodsInfo", currgoods);
                                    //Txtfqty_KeyPress(sender, e);
                                    for (int i = 0; i < resultList.Count; i++)
                                    {
                                        if (resultList[i].id1 == currgoods.id1)
                                        {
                                            resultList[i].ffhqty = currgoods.ffhqty;
                                            break;
                                        }
                                    }
                                    //}
                                }
                            }
                            _originalAdapter.UpdateData(resultList);
                            this.txtInput.Text = "";
                            e.Handled = true;
                            return;
                        }
                        else if (ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
                        {
                            txtInput.Text = ChooseGoodsInfo[0].cgoodsid;
                            if (currgoods != null && currgoods.cgoodsid != ChooseGoodsInfo[0].cgoodsid)
                            {
                                ClearFormText();
                            }
                        }
                        else if (dialogUDIList != null && dialogUDIList.Count > 1)//下拉选择
                        {
                            e.Handled = true;
                            return;
                        }
                        if (ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
                        {
                            if (currgoods != null && (currgoods.cgoodsid != ChooseGoodsInfo[0].cgoodsid || currgoods.cph != ChooseGoodsInfo[0].cph))//KB023 2022-12-03 当切换品种批号时，清空网格的值
                            {
                                ClearFormText();
                            }
                        }
                    }
                    else
                    {
                        e.Handled = true;
                        return;
                    }
                }

                #endregion

                #region 扫描二维码
                if (DealSCanRQCode())
                {
                    if (dialogQRList != null && dialogQRList.Count > 1)//多选处理
                    {
                        e.Handled = true;
                        return;
                    }
                }
                #endregion


                txtQuery_Click(null, null);
                e.Handled = true;
            }
        }

        bool isPopuChooseForm = false;
        void txtQuery_Click(object sender, EventArgs e)
        {
            if (isPopuChooseForm)
            {
                return;
            }
            resultList2 = new List<XCSTOCKGoods>();
            isPopuChooseForm = true;
            var txt = txtInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(txt))
            {
                Toast.MakeText(this, "查询内容不能为空！", ToastLength.Long).Show();
                isPopuChooseForm = false;
                return;
            }
            txtInput.Text = "";
            if (bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && ChooseGoodsInfo[0].cgoodsid == txt && resultList != null && resultList.Count > 0 && !resultList.Where(a => a.cgoodsid == txt).Any())
            {
                Toast.MakeText(this, "出库复核单不存在该商品信息，请检查！", ToastLength.Long).Show();//KB023 2022-08-17
                ChooseGoodsInfo = new List<InstrumentGoodsInfo>();
                isPopuChooseForm = false;
                return;
            }
            var dt = DataTableHelper.ToDataTabel<XCSTOCKGoods>(resultList);
            var strs = string.Format("cgoodsid like '%{0}%' or cgoodsname like '%{0}%' or ccommonname like '{0}' or czjmcode like '%{0}%' or cbarcode like '%{0}%' or cbarcode1 like '%{0}%' or cbarcode2 like '%{0}%'", txt);
            if (dt.Select(strs).Any())
            {
                var tmplist = dt.Select(strs).ToList();
                for (int i = 0; i < tmplist.Count; i++)
                {
                    var goodsInfo = DataTableHelper.DataRowToClass<XCSTOCKGoods>(tmplist[i]);
                    resultList2.Add(goodsInfo);
                }
            }

            if (resultList2.Count > 0 && bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
            {
                var tmpXCGoods = resultList2.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph == ChooseGoodsInfo[0].cph).FirstOrDefault();
                if (iSamePHProcess > 1)
                {
                    if (!string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].dmadedate) && !string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].dexpdate))
                    {
                        tmpXCGoods = resultList2.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph.Contains(ChooseGoodsInfo[0].cph) && a.dmadedate == ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate) && a.dexpdate == ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate)).FirstOrDefault();
                    }
                    else if (!string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].dmadedate) && string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].dexpdate))
                    {
                        var resultList3 = resultList2.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph.Contains(ChooseGoodsInfo[0].cph) && a.dmadedate == ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate)).ToList();
                        if (resultList3 != null && resultList3.Count > 1)
                        {
                            mtype = 3;
                            var xcinfodt = DataTableHelper.ToDataTabel(resultList3);
                            phpopWindowList = xcinfodt.Select("").ToList();

                            popUDIWindowAdapter = new CommonPopwindowAdapter<DataRow>(this, phpopWindowList, this, Resource.Layout.RrCodeChose);
                            popUDIWindowAdapter.SetListViewColumn += (position, convertView, parent, row, viewHolder) =>
                            {
                                viewHolder.SetText(Resource.Id.textView1, row["cgoodsid"].ToString());//+ "\\"+row["cgoodsid"].ToString()
                                viewHolder.SetText(Resource.Id.textView2, row["cph"].ToString());
                                viewHolder.SetText(Resource.Id.textView3, row["dmadedate"].ToString());
                                viewHolder.SetText(Resource.Id.textView4, row["dexpdate"].ToString());
                                return viewHolder.GetConvertView();
                            };
                            popUDIWindowAdapter.SetOnDismissListener(this);
                            //popWindowAdapter.Width = txtcBarcode.Width;
                            popUDIWindowAdapter.ShowAsDropDown(txtInput);
                            return;
                        }
                        else
                        {
                            tmpXCGoods = resultList3 != null ? resultList3.FirstOrDefault() : null;
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].dmadedate) && !string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].dexpdate))
                    {
                        var resultList3 = resultList2.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph.Contains(ChooseGoodsInfo[0].cph) && a.dexpdate == ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate)).ToList();
                        if (resultList3 != null && resultList3.Count > 1)
                        {
                            mtype = 3;
                            var xcinfodt = DataTableHelper.ToDataTabel(resultList3);
                            phpopWindowList = xcinfodt.Select("").ToList();

                            popUDIWindowAdapter = new CommonPopwindowAdapter<DataRow>(this, phpopWindowList, this, Resource.Layout.RrCodeChose);
                            popUDIWindowAdapter.SetListViewColumn += (position, convertView, parent, row, viewHolder) =>
                            {
                                viewHolder.SetText(Resource.Id.textView1, row["cgoodsid"].ToString());//+ "\\"+row["cgoodsid"].ToString()
                                viewHolder.SetText(Resource.Id.textView2, row["cph"].ToString());
                                viewHolder.SetText(Resource.Id.textView3, row["dmadedate"].ToString());
                                viewHolder.SetText(Resource.Id.textView4, row["dexpdate"].ToString());
                                return viewHolder.GetConvertView();
                            };
                            popUDIWindowAdapter.SetOnDismissListener(this);
                            //popWindowAdapter.Width = txtcBarcode.Width;
                            popUDIWindowAdapter.ShowAsDropDown(txtInput);
                            return;
                        }
                        else
                        {
                            tmpXCGoods = resultList3 != null ? resultList3.FirstOrDefault() : null;
                        }
                    }
                    else
                    {
                        var resultList3 = resultList2.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph.Contains(ChooseGoodsInfo[0].cph)).ToList();
                        if (resultList3 != null && resultList3.Count > 1)
                        {
                            mtype = 3;
                            var xcinfodt = DataTableHelper.ToDataTabel(resultList3);
                            phpopWindowList = xcinfodt.Select("").ToList();

                            popUDIWindowAdapter = new CommonPopwindowAdapter<DataRow>(this, phpopWindowList, this, Resource.Layout.RrCodeChose);
                            popUDIWindowAdapter.SetListViewColumn += (position, convertView, parent, row, viewHolder) =>
                            {
                                viewHolder.SetText(Resource.Id.textView1, row["cgoodsid"].ToString());//+ "\\"+row["cgoodsid"].ToString()
                                viewHolder.SetText(Resource.Id.textView2, row["cph"].ToString());
                                viewHolder.SetText(Resource.Id.textView3, row["dmadedate"].ToString());
                                viewHolder.SetText(Resource.Id.textView4, row["dexpdate"].ToString());
                                return viewHolder.GetConvertView();
                            };
                            popUDIWindowAdapter.SetOnDismissListener(this);
                            //popWindowAdapter.Width = txtcBarcode.Width;
                            popUDIWindowAdapter.ShowAsDropDown(txtInput);
                            return;
                        }
                        else
                        {
                            tmpXCGoods = resultList3 != null ? resultList3.FirstOrDefault() : null;
                        }
                    }
                }
                if (tmpXCGoods == null)
                {
                    Toast.MakeText(this, "没有对应的批号信息！", 0).Show();
                    isPopuChooseForm = false;
                }
                else
                {
                    //如果已经存在已确认的品种批号，则需要赋值id1
                    if (resultList != null && resultList.Count > 0 && iSamePHProcess > 1 && resultList.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph.Contains(tmpXCGoods.cph) && a.dmadedate == tmpXCGoods.dmadedate && a.dexpdate == tmpXCGoods.dexpdate && a.id1 != 0).Any())
                    {
                        var tmpresult = resultList.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph.Contains(tmpXCGoods.cph) && a.dmadedate == tmpXCGoods.dmadedate && a.dexpdate == tmpXCGoods.dexpdate && a.id1 != 0).ToList();

                        if (GlobalDataCache.p_SOFTUSERTYPE == "zz579" && strcbiltype == "CC" && bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)//   KB023 2024-03-28  
                        {
                            var tmpresult2 = resultList.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph.Contains(tmpXCGoods.cph) && a.dmadedate == tmpXCGoods.dmadedate && a.dexpdate == tmpXCGoods.dexpdate && a.creserve1 == ChooseGoodsInfo[0].cudicode && a.id1 != 0).ToList();
                            if (tmpresult2 != null && tmpresult2.Count > 0)
                            {
                                tmpresult = tmpresult2;
                            }
                        }
                        seldialogId1 = tmpresult[0].id1;
                        tmpXCGoods.ffhqty = tmpresult[0].ffhqty;//复核数量
                        tmpXCGoods.id1 = seldialogId1;
                        selDelId1 = seldialogId1;
                        /*
                         * FindViewById<TextView>(Resource.Id.txtscrq).Text = info.dmadedate;
                FindViewById<TextView>(Resource.Id.txtyxrq).Text = info.dexpdate;
                         */
                        if (FindViewById<TextView>(Resource.Id.txtcgoodsname).Text != tmpXCGoods.cgoodsname || !tmpXCGoods.cph.Contains(FindViewById<TextView>(Resource.Id.txtcph).Text) || tmpXCGoods.dmadedate != FindViewById<TextView>(Resource.Id.txtscrq).Text || tmpXCGoods.dexpdate != FindViewById<TextView>(Resource.Id.txtyxrq).Text)
                        {
                            SetNewFormText(tmpXCGoods);
                        }
                    }
                    else if (resultList != null && resultList.Count > 0 && iSamePHProcess <= 1 && resultList.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph == tmpXCGoods.cph && a.id1 != 0).Any())
                    {
                        var tmpresult = resultList.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph == tmpXCGoods.cph && a.id1 != 0).ToList();

                        if (GlobalDataCache.p_SOFTUSERTYPE == "zz579" && strcbiltype == "CC" && bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)//   KB023 2024-03-28  
                        {
                            var tmpresult2 = resultList.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph == tmpXCGoods.cph && a.creserve1 == ChooseGoodsInfo[0].cudicode && a.id1 != 0).ToList();
                            if (tmpresult2 != null && tmpresult2.Count > 0)
                            {
                                tmpresult = tmpresult2;
                            }
                        }

                        seldialogId1 = tmpresult[0].id1;
                        tmpXCGoods.ffhqty = tmpresult[0].ffhqty;//复核数量
                        tmpXCGoods.id1 = seldialogId1;
                        selDelId1 = seldialogId1;
                        if (FindViewById<TextView>(Resource.Id.txtcgoodsname).Text != tmpXCGoods.cgoodsname && FindViewById<TextView>(Resource.Id.txtcph).Text != tmpXCGoods.cph)
                        {
                            SetNewFormText(tmpXCGoods);
                        }
                    }
                    //更新数量 
                    var bresult = false;

                    var tmp = CoumTracefqty(tmpXCGoods, "check", ref bresult);

                    if (tmp + tmpXCGoods.finputqty > tmpXCGoods.fnoticeqty)
                    {
                        Toast.MakeText(this, "出库复核数量[" + tmp.ToString() + "]+复核异常数量[" + ConvertHelper.ToDecimal(tmpXCGoods.finputqty).ToString() + "]不能大于通知数量数量[" + tmpXCGoods.fnoticeqty.ToString() + "]！", 0).Show();
                        isPopuChooseForm = false;
                        return;
                    }
                    tmpXCGoods.ffhqty = tmp;
                    //tmpXCGoods.fnormvalue = ConvertHelper.ToDecimal(tmp) * ConvertHelper.ToDecimal(txtprice.Text);
                    if (!bresult)
                    {
                        AddCurrentEditTraceList(ref tmpXCGoods);
                    }
                    // 判断是否存在序列号
                    if (CurrentEditTrace12List != null && iSamePHProcess <= 0 && CurrentEditTrace12List.Count > 0 && CurrentEditTrace12List.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph == tmpXCGoods.cph).Any())
                    {
                        // tmp = tmp;
                    }
                    else if (iSamePHProcess > 1 && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && resultList.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph == tmpXCGoods.cph && a.dmadedate == tmpXCGoods.dmadedate && a.dexpdate == tmpXCGoods.dexpdate && a.ref_cbilid == tmpXCGoods.ref_cbilid && a.id1 == tmpXCGoods.id1).Any())
                    {
                        var tmpresult = resultList.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph == tmpXCGoods.cph && a.dmadedate == tmpXCGoods.dmadedate && a.dexpdate == tmpXCGoods.dexpdate && a.ref_cbilid == tmpXCGoods.ref_cbilid && a.id1 == tmpXCGoods.id1).FirstOrDefault();
                        if (tmpresult != null)
                        {
                            tmp = tmpresult.ffhqty + ChooseGoodsInfo[0].fqty * scanCount;
                        }
                        else
                        {
                            tmp = ConvertHelper.ToDecimal(this.txtfqty.Text) + ChooseGoodsInfo[0].fqty * scanCount;
                        }
                    }
                    else if (iSamePHProcess <= 1 && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && resultList.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph == tmpXCGoods.cph && a.ref_cbilid == tmpXCGoods.ref_cbilid && a.id1 == tmpXCGoods.id1).Any())
                    {
                        var tmpresult = resultList.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph == tmpXCGoods.cph && a.ref_cbilid == tmpXCGoods.ref_cbilid && a.id1 == tmpXCGoods.id1).FirstOrDefault();
                        if (tmpresult != null)
                        {
                            tmp = tmpresult.ffhqty + ChooseGoodsInfo[0].fqty * scanCount;
                        }
                        else
                        {
                            tmp = ConvertHelper.ToDecimal(this.txtfqty.Text) + ChooseGoodsInfo[0].fqty * scanCount;
                        }
                    }
                    else
                    {
                        //如果没有扫码，则

                        tmp = ConvertHelper.ToDecimal(this.txtfqty.Text) + ChooseGoodsInfo[0].fqty * scanCount;
                    }
                    tmpXCGoods.ffhqty = tmp;
                    if (!bresult)
                    {
                        //this.txtfqty.Text = tmp.ToString(GlobalDataCache.QtyFormat);
                        //this.txtvalue.Text = (ConvertHelper.ToDecimal(txtfqty.Text) * ConvertHelper.ToDecimal(txtprice.Text)).ToString(GlobalDataCache.ValueFormat);
                        GlobalDataCache.SetData("PDACKFHDGoodsInfo", tmpXCGoods);
                    }
                    SetNewFormText(tmpXCGoods);
                    if (tmpXCGoods != null)
                    {
                        for (int i = 0; i < resultList.Count; i++)
                        {
                            if (resultList[i].id1 == tmpXCGoods.id1)
                            {
                                resultList[i].ffhqty = tmpXCGoods.ffhqty;
                                break;
                            }
                        }
                        _originalAdapter.UpdateData(resultList);
                    }
                    this.txtInput.Focusable = true;     //定位到品种扫描
                    this.txtInput.FocusableInTouchMode = true;
                    this.txtInput.RequestFocus();
                }
            }
            else if (resultList2.Count > 0)
            {
                SetNewFormText(resultList2[0]);
            }
            else
            {
                Toast.MakeText(this, "没有对应的商品信息！", 0).Show();
            }
            isPopuChooseForm = false;
        }

        private void Txtfqty_KeyPress(object sender, View.KeyEventArgs e)
        {
            e.Handled = false;
            var currgoods = GlobalDataCache.GetData<XCSTOCKGoods>("PDACKFHDGoodsInfo");
            if (currgoods == null)
            {
                return;
            }
            if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Up)
            {
                decimal fqty = 0;
                decimal reffqty = 0;
                if (string.IsNullOrWhiteSpace(txtfqty.Text))
                {
                    Toast.MakeText(this, "出库复核数量不能为空！", 0).Show();
                    return;
                }
                if (!decimal.TryParse(txtfqty.Text, out fqty))
                {
                    Toast.MakeText(this, "出库复核数量格式不正确！", 0).Show();
                    return;
                }
                txtfqty.Text = fqty.ToString(GlobalDataCache.QtyFormat);
                fqty = Convert.ToDecimal(fqty);
                if (fqty <= 0)
                {
                    Toast.MakeText(this, "出库复核数量必须大于0！", 0).Show();
                    return;
                }
                if (fqty >= 1000000)
                {
                    Toast.MakeText(this, "出库复核数量必须小于一百万！", 0).Show();
                    return;
                }
                if (fqty > currgoods.ref_fqty && !string.IsNullOrEmpty(currgoods.ref_ctabname) && currgoods.ref_fqty > 0)
                {
                    Toast.MakeText(this, "出库复核数量[" + fqty.ToString() + "]不能大于源单数量[" + currgoods.ref_fqty.ToString() + "]！", 0).Show();
                    return;
                }
                if (IsEnableSFDARenewal)
                {
                    GetFILETEXT(currgoods);
                    currgoods.cphnote2 = this.txtFILE.Text;
                }
                if (currgoods.fqty != fqty)
                {
                    currgoods.fqty = fqty;
                    currgoods.fnormvalue = fqty * Convert.ToDecimal(this.txtprice.Text);
                    this.txtvalue.Text = currgoods.fnormvalue.ToString(GlobalDataCache.ValueFormat);
                    GlobalDataCache.SetData("PDACKFHDGoodsInfo", currgoods);
                }

                //SyncGoodsItems(currgoods, fqty);
                e.Handled = true;
                this.txtvalue.Focusable = true;
                this.txtvalue.FocusableInTouchMode = true;
                this.txtvalue.RequestFocus();
                GC.Collect();
            }
        }
        private void GetFILETEXT(XCSTOCKGoods item)
        {
            try
            {
                if (item == null) return;
                if (string.IsNullOrEmpty(item.dmadedate)) return;
                if (cfilenofilelist == null) return;
                if (cfilenofilelist.Where(a => a.cgoodsid == item.cgoodsid).Any())
                {
                    var filelist = cfilenofilelist.Where(a => a.cgoodsid == item.cgoodsid && ConvertHelper.ToDateTime(a.dfilenodate) >= ConvertHelper.ToDateTime(item.dmadedate)).OrderBy(d => ConvertHelper.ToDateTime(d.dfilenodate)).ToList();
                    if (filelist != null && filelist.Count > 0)
                    {
                        txtFILE.Text = ConvertHelper.ToString(filelist.FirstOrDefault().cfileno);
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }

        }
        private bool checkvalue()
        {
            if (!string.IsNullOrWhiteSpace(txtvalue.Text))
            {
                decimal fvalue = 0;
                if (!string.IsNullOrWhiteSpace(txtvalue.Text) && !decimal.TryParse(txtvalue.Text, out fvalue))
                {
                    Toast.MakeText(this, "金额格式不正确！", 0).Show();
                    return false;
                }
                txtvalue.Text = fvalue.ToString(GlobalDataCache.QtyFormat);
                fvalue = Convert.ToDecimal(fvalue);
                //KB023 2022-08-04 判断数量
                if (!string.IsNullOrEmpty(cgoodsid) && resultList != null && resultList.Count > 0)
                {
                    var tmpfqty = resultList.Where(a => a.cgoodsid == cgoodsid && a.id1 > 0 && a.iflag == 100 && a.cph != txtcph.Text).Sum(p => p.fnormvalue);
                    fvalue += tmpfqty;
                }
                if (fvalue <= 0)
                {
                    Toast.MakeText(this, "出库复核金额必须大于0！", 0).Show();
                    return false;
                }
                if (fvalue >= 1000000)
                {
                    Toast.MakeText(this, "出库复核金额必须小于一百万！", 0).Show();
                    return false;
                }
            }
            return true;

        }

        private void SyncGoodsItems(XCSTOCKGoods info, decimal fqty)
        {
            if (XCSTOCKMainInfo.cbilid != "Add")
            {
                this.Proxy.Execute(new PDAXCSTOCKOPRequest()
                {
                    OrgID = GlobalProxySetting.OrgID,
                    OPType = 3,
                    cbilid = XCSTOCKMainInfo.cbilid,
                    LRInfoTrace12List = CurrentEditTrace12List,
                    LRInfoTrace13List = CurrentEditTrace13List,
                    Goodsinfo = info,
                    QueryText = fqty.ToString()
                },
                 (response) =>
                 {
                     if (!response.IsError)
                     {
                         loadform();
                     }
                 },
                 this);
            }
        }

        private void loadform()
        {
            this.Proxy.Execute(new PDAXCSTOCKOPRequest()
            {
                OrgID = GlobalProxySetting.OrgID,
                OPType = 1,
                cbilid = XCSTOCKMainInfo.cbilid
            },
            (response) =>
            {
                if (response != null && !response.IsError)
                {
                    resultList = response.GoodsList.Where(a => a.iflag <= 100).OrderBy(b => b.iflag).ThenBy(b => b.cgoodsid).ToList();
                    //resultList2 = resultList;
                    //resultList = resultList.Where(a => a.iflag < 100).OrderBy(b => b.iflag).ToList();

                    if (resultList != null)
                    {
                        if (IsUseNewScanFlow)
                        {
                            if (chkUDI != null && chkUDI.Visibility == ViewStates.Visible)
                            {
                                foreach (var item in resultList)
                                {
                                    //var ffhqty = response.LRInfoTrace12List.Where(r => r.cgoodsid == item.cgoodsid && r.cph == item.cph && r.dmadedate == item.dmadedate && r.dexpdate == item.dexpdate).Sum(r => r.fzsmqty);
                                    var ffhqty = response.LRInfoTrace12List.Where(r => r.id1 == item.id1).Sum(r => r.fzsmqty);

                                    var sqlDATA = "SELECT * FROM bf_ckfh_data WHERE cbilid=@cbilid AND id1=@id1";
                                    var tempdata = GlobalDataCache.DBAccess.GetDataTable(sqlDATA
                                        , GlobalDataCache.DBAccess.CreateDbParameter("@cbilid", item.cbilid)
                                        , GlobalDataCache.DBAccess.CreateDbParameter("@id1", item.id1));
                                    if (tempdata != null && tempdata.Rows.Count > 0)
                                    {
                                        sqlDATA = @"UPDATE bf_ckfh_data
                                             SET ffhqty=@ffhqty,
                                             cfher1=@cfher1,
                                             cfher2=@cfher2,
                                             iuserid=@iuserid,
                                             ccomputername=@ccomputername
                                             WHERE cbilid=@cbilid AND id1=@id1";
                                    }
                                    else
                                    {
                                        //如果从码采集中获取到的数量为0则不作处理
                                        if (ffhqty <= 0)
                                        {
                                            continue;
                                        }
                                        sqlDATA = @"INSERT INTO bf_ckfh_data(
	                                            cbilid
	                                            ,id1
	                                            ,ffhqty
	                                            ,ccancelseason
	                                            ,cfher1
	                                            ,cfher2
	                                            ,dfhdatetime
	                                            ,iuserid
	                                            ,ccomputername)
	                                            VALUES(
	                                            @cbilid
	                                            ,@id1
	                                            ,@ffhqty
	                                            ,@ccancelseason
	                                            ,@cfher1
	                                            ,@cfher2
	                                            ,@dfhdatetime
	                                            ,@iuserid
	                                            ,@ccomputername)";
                                    }
                                    GlobalDataCache.DBAccess.ExecuteNonQuery(sqlDATA,
                                                                     GlobalDataCache.DBAccess.CreateDbParameter("@cbilid", item.cbilid)
                                                                     , GlobalDataCache.DBAccess.CreateDbParameter("@id1", item.id1)
                                                                     , GlobalDataCache.DBAccess.CreateDbParameter("@ffhqty", ffhqty)
                                                                     , GlobalDataCache.DBAccess.CreateDbParameter("@ccancelseason", string.Empty)
                                                                     , GlobalDataCache.DBAccess.CreateDbParameter("@cfher1", GlobalProxySetting.UserID)
                                                                     , GlobalDataCache.DBAccess.CreateDbParameter("@cfher2", "")
                                                                     , GlobalDataCache.DBAccess.CreateDbParameter("@dfhdatetime", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                                                                     , GlobalDataCache.DBAccess.CreateDbParameter("@iuserid", 0)
                                                                     , GlobalDataCache.DBAccess.CreateDbParameter("@ccomputername", GlobalDataCache.Imei)
                                                                     );
                                }

                            }
                        }

                        //var tempSaveRequest = new BusinessRequest() { BusinessKey = "UpdateChkDetailGoodsProcess" };
                        //tempSaveRequest.Parameters["opType"] = "loadData";
                        //tempSaveRequest.Parameters["cbilid"] = XCSTOCKMainInfo.cbilid;
                        //var tempSaveResponse = this.Proxy.Execute(tempSaveRequest);
                        //if (tempSaveResponse != null && !tempSaveResponse.IsError)
                        //{
                        //var tempData = tempSaveResponse.Result["data"] as DataTable;
                        var sql = "select * from bf_ckfh_data where cbilid=@cbilid";
                        var tempData = GlobalDataCache.DBAccess.GetDataTable(sql, GlobalDataCache.DBAccess.CreateDbParameter("@cbilid", XCSTOCKMainInfo.cbilid));
                        if (tempData.Rows.Count > 0)
                        {
                            dtTempData = tempData;
                            CustomAlertDialog cadtempData = new CustomAlertDialog();
                            cadtempData.OKClick += CadtempData_OKClick;
                            cadtempData.AlertDialogShow(this, "当前单据有临时保存的复核数据，是否加载？");
                            cadtempData.CancelClick += (s, e) =>
                            {

                                if (IsUseNewScanFlow)
                                {
                                    //存在复核数据
                                    for (int i = 0; i < resultList.Count; i++)
                                    {
                                        resultList[i].ffhqty = 0;
                                        resultList[i].finputqty = 0;
                                        resultList[i].fcancelqty = 0;
                                        resultList[i].ccancelseason = "";

                                        var id1 = resultList[i].id1;
                                        var tempSaveRequest = new BusinessRequest() { BusinessKey = "UpdateChkDetailGoodsProcess" };
                                        tempSaveRequest.Parameters["opType"] = "clear";
                                        tempSaveRequest.Parameters["cbilid"] = XCSTOCKMainInfo.cbilid;
                                        var tempSaveResponse = this.Proxy.Execute(tempSaveRequest);
                                        if (tempSaveResponse != null && tempSaveResponse.IsError)
                                        {
                                            Toast.MakeText(this, "清空出库复核临时保存数据出错！", 0).Show();
                                            return;
                                        }
                                        deleteckfh_data();
                                        if (bchkUDI || (CurrentEditTrace12List != null && CurrentEditTrace12List.Count() > 0))
                                        {
                                            if (CurrentEditTrace12List != null && CurrentEditTrace12List.Where(a => a.id1 == id1).Any())
                                            {
                                                var TempEditTrace12 = CurrentEditTrace12List.Where(a => a.id1 == id1).ToList();
                                                for (int j = 0; j < TempEditTrace12.Count; j++)
                                                {
                                                    CurrentEditTrace12List.Remove(TempEditTrace12[j]);
                                                }

                                            }
                                            if (CurrentEditTrace13List != null && CurrentEditTrace13List.Where(a => a.id1 == id1).Any())
                                            {
                                                var TempEditTrace13 = CurrentEditTrace13List.Where(a => a.id1 == id1).ToList();
                                                for (int j = 0; j < TempEditTrace13.Count; j++)
                                                {
                                                    CurrentEditTrace13List.Remove(TempEditTrace13[j]);
                                                }

                                            }
                                        }
                                    }

                                    _originalAdapter.UpdateData(resultList);
                                }
                            };
                        }
                        //}

                        if (resultList.Count != 0)
                        {
                            SetFormText(resultList[0]);
                            GlobalDataCache.SetData("PDACKFHDGoodsInfo", resultList[0]);
                        }
                        _originalAdapter.UpdateData(resultList);
                    }
                    if (chkUDI != null && chkUDI.Visibility == ViewStates.Visible)
                    {
                        CurrentEditTrace12List = response.LRInfoTrace12List;
                        CurrentEditTrace13List = response.LRInfoTrace13List;
                    }
                }
            },
            this);

        }

        private void CadtempData_OKClick(object sender, EventArgs e)
        {
            if (resultList != null && resultList.Count > 0 && dtTempData != null && dtTempData.Rows.Count > 0)
            {
                for (int i = 0; i < resultList.Count; i++)
                {
                    var newRow = dtTempData.Select(string.Format("id1='{0}'", resultList[i].id1)).FirstOrDefault();
                    if (newRow != null)
                    {
                        resultList[i].ffhqty = ConvertHelper.ToDecimal(newRow["ffhqty"]);
                        resultList[i].fcancelqty = Convert.ToDecimal(resultList[i].fnoticeqty) - Convert.ToDecimal(newRow["ffhqty"]);
                        resultList[i].ccancelseason = ConvertHelper.ToString(newRow["ccancelseason"]);
                    }
                }
                if (resultList.Count != 0)
                {
                    SetFormText(resultList[0]);
                    GlobalDataCache.SetData("PDACKFHDGoodsInfo", resultList[0]);
                }
                _originalAdapter.UpdateData(resultList);
            }
        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            for (int i = 0; i < e.Parent.ChildCount; i++)
            {
                var cv = e.Parent.GetChildAt(i);
                //cv.SetBackgroundResource(0);
            }
            //(e.View).SetBackgroundResource(Resource.Drawable.rectangle);
            //(e.View).SetBackgroundColor(custom_blue);
            SetFormText(((e.View).Tag as CKFHDGoodsInfoViewHolder).Data);
            GlobalDataCache.SetData("PDACKFHDGoodsInfo", ((e.View).Tag as CKFHDGoodsInfoViewHolder).Data);
        }
        private void TB_NavigationOnClick(object sender, EventArgs e)
        {
            this.ReleaseBillEdit("WMSM02001103", XCSTOCKMainInfo.cbilid);
            GlobalDataCache.SetData("PDACKFHDGoodsInfo", null);
            Finish();
        }

        #region 
        private void SetNewFormText(XCSTOCKGoods info)
        {
            MainGoodsInfo = info;
            //存在品种批号时，id1不需要加1
            if (resultList.Where(a => a.cgoodsid == info.cgoodsid && a.cph == info.cph && a.id1 != 0).Any())
            {
                var tmpresult = resultList.Where(a => a.cgoodsid == info.cgoodsid && a.cph == info.cph && a.id1 != 0).ToList();
                if (GlobalDataCache.p_SOFTUSERTYPE == "zz579" && strcbiltype == "CC" && bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && ConvertHelper.ToString(info.creserve1) != "")//   KB023 2024-03-28 
                {
                    var tmpresult2 = resultList.Where(a => a.cgoodsid == info.cgoodsid && a.cph == info.cph && a.creserve1 == info.creserve1 && a.id1 != 0).ToList();
                    if (tmpresult2 != null && tmpresult2.Count > 0)
                    {
                        tmpresult = tmpresult2;
                    }
                }
                seldialogId1 = tmpresult[0].id1;
                selDelId1 = seldialogId1;
            }
            else
            {
                seldialogId1 = resultList.Count + 1;
                selDelId1 = resultList.Count + 1;
                MainGoodsInfo.id1 = seldialogId1;
            }
            SetFormText(info);
        }
        #endregion

        #region 出库复核单商品明细详情
        private void SetFormText(XCSTOCKGoods info)
        {
            if (info != null)
            {
                seldialogId1 = info.id1 == 0 ? seldialogId1 : info.id1;
                selDelId1 = info.id1 == 0 ? selDelId1 : info.id1;
                cgoodsid = info.cgoodsid;
                FindViewById<TextView>(Resource.Id.txtcbilid).Text = info.cbilid;

                FindViewById<TextView>(Resource.Id.txtcgoodsname).Text = info.ccommonname;
                FindViewById<TextView>(Resource.Id.txtdw).Text = info.cunit;
                FindViewById<TextView>(Resource.Id.txtcpkname).Text = info.cpkname;
                FindViewById<TextView>(Resource.Id.txtcfactoryname).Text = info.cfactoryname;
                FindViewById<TextView>(Resource.Id.txtscrq).Text = info.dmadedate;
                FindViewById<TextView>(Resource.Id.txtyxrq).Text = info.dexpdate;
                FindViewById<TextView>(Resource.Id.txtcph).Text = info.cph;
                FindViewById<TextView>(Resource.Id.txtfnoticeqty).Text = info.fnoticeqty.ToString(GlobalDataCache.QtyFormat);

                txt_scxkz.Text = ConvertHelper.ToString(info.ccertificateno);//V1.2

                if (IsEnableSFDARenewal)
                {
                    cfilenofilelist = info.cfilenofilelist;
                    if (cfilenofilelist != null && cfilenofilelist.Count > 0)
                    {
                        addcfileno(info.cgoodsid);
                    }
                    this.txtFILE.Text = ConvertHelper.ToString(info.cphnote2) != "" ? info.cphnote2 : info.cfileno;
                }
                else
                {
                    this.txtFILE.Text = info.cfileno;
                }
                MainGoodsInfo = info;
            }
            Showtxtfinish();
            //SetFormEditEnable();
        }
        private void Showtxtfinish()
        {
            var resultfinishList = resultList.Where(a => (a.ffhqty > 0 || a.finputqty > 0)).ToList();
            var finishqty = "";
            finishqty = resultfinishList.Count.ToString() + "/" + resultList.Count.ToString();
            FindViewById<TextView>(Resource.Id.txtfinish).Text = finishqty;
        }
        #endregion
        //添加所属注册证号
        private void addcfileno(string cgoodsid)
        {
            cfilenogoodfilelist = new List<string>();
            var filelist = new List<PDAGoodscfilenofilelistRequest>();
            if (cfilenofilelist.Where(a => a.cgoodsid == cgoodsid).Any())
            {
                filelist = cfilenofilelist.Where(a => a.cgoodsid == cgoodsid).ToList();
            }
            if (filelist.Count > 0)
            {
                for (int i = 0; i < filelist.Count; i++)
                {
                    cfilenogoodfilelist.Add(filelist[i].cfileno);
                }
            }
            spinnerPopcfileno = new SpinnerPopWindowAdapter<string>(this, cfilenogoodfilelist, this);
            spinnerPopcfileno.SetOnDismissListener(this);
            txtFILE.Click += (s, e) =>
            {
                mtype = 2;
                spinnerPopcfileno.Width = txtFILE.Width;
                spinnerPopcfileno.ShowAsDropDown(txtFILE);
                //SetTextImage(Resource.Drawable.icon_up);
            };
        }
        private void SetFormEditEnable(string sFlag = "")
        {
            if (sFlag == "edit")
            {
                this.txtcph.Enabled = true;
                this.txtfqty.Enabled = true;
                this.txtSCDATE.Enabled = true;
                this.txtYXDATE.Enabled = true;
                this.txtvalue.Enabled = true;
            }
            else
            {
                this.txtcph.Enabled = false;
                this.txtfqty.Enabled = false;
                this.txtSCDATE.Enabled = false;
                this.txtYXDATE.Enabled = false;
                this.txtvalue.Enabled = false;
            }
            this.txtprice.Enabled = false;
        }
        private void deleteckfh_data()
        {
            var sql = "delete from bf_ckfh_data where cbilid=@cbilid and ccomputername=@ccomputername";
            GlobalDataCache.DBAccess.ExecuteNonQuery(sql
                , GlobalDataCache.DBAccess.CreateDbParameter("@cbilid", XCSTOCKMainInfo.cbilid)
                , GlobalDataCache.DBAccess.CreateDbParameter("@ccomputername", GlobalDataCache.Imei));
        }
        private void ClearFormText(string sFlag = "")
        {
            if (MainGoodsInfo != null && sFlag == "clear" && resultList != null && resultList.Count > 0 && resultList.Where(a => a.ffhqty > 0 || a.finputqty > 0).Any())
            {

                //存在复核数据
                for (int i = 0; i < resultList.Count; i++)
                {
                    if (MainGoodsInfo.id1 == resultList[i].id1)
                    {
                        resultList[i].ffhqty = 0;
                        resultList[i].finputqty = 0;
                        resultList[i].fcancelqty = 0;
                        resultList[i].ccancelseason = "";
                        var tempSaveRequest = new BusinessRequest() { BusinessKey = "UpdateChkDetailGoodsProcess" };
                        tempSaveRequest.Parameters["opType"] = "clear";
                        tempSaveRequest.Parameters["cbilid"] = XCSTOCKMainInfo.cbilid;
                        var tempSaveResponse = this.Proxy.Execute(tempSaveRequest);
                        if (tempSaveResponse != null && tempSaveResponse.IsError)
                        {
                            Toast.MakeText(this, "清空出库复核临时保存数据出错！", 0).Show();
                            return;
                        }
                        deleteckfh_data();
                        if (bchkUDI || (CurrentEditTrace12List != null && CurrentEditTrace12List.Count() > 0))
                        {
                            if (CurrentEditTrace12List != null && CurrentEditTrace12List.Where(a => a.id1 == MainGoodsInfo.id1).Any())
                            {
                                var TempEditTrace12 = CurrentEditTrace12List.Where(a => a.id1 == MainGoodsInfo.id1).ToList();
                                for (int j = 0; j < TempEditTrace12.Count; j++)
                                {
                                    CurrentEditTrace12List.Remove(TempEditTrace12[j]);
                                }

                            }
                            if (CurrentEditTrace13List != null && CurrentEditTrace13List.Where(a => a.id1 == MainGoodsInfo.id1).Any())
                            {
                                var TempEditTrace13 = CurrentEditTrace13List.Where(a => a.id1 == MainGoodsInfo.id1).ToList();
                                for (int j = 0; j < TempEditTrace13.Count; j++)
                                {
                                    CurrentEditTrace13List.Remove(TempEditTrace13[j]);
                                }

                            }
                        }
                    }
                }
                _originalAdapter.UpdateData(resultList);
            }

            selDelId1 = -1;
            seldialogId1 = -1;
            cgoodsid = "";
            FindViewById<TextView>(Resource.Id.txtcbilid).Text = "";
            FindViewById<TextView>(Resource.Id.txtfinish).Text = "";
            FindViewById<TextView>(Resource.Id.txtcgoodsname).Text = "";
            FindViewById<TextView>(Resource.Id.txtcpkname).Text = "";
            FindViewById<TextView>(Resource.Id.txtdw).Text = "";
            FindViewById<TextView>(Resource.Id.txtcfactoryname).Text = "";
            FindViewById<TextView>(Resource.Id.txtcph).Text = "";
            FindViewById<TextView>(Resource.Id.txtfnoticeqty).Text = "";
            FindViewById<TextView>(Resource.Id.txtscrq).Text = "";
            FindViewById<TextView>(Resource.Id.txtyxrq).Text = "";
            txtFILE.Text = "";
            txt_scxkz.Text = string.Empty;//V1.2
            listcph = null;
            MainGoodsInfo = null;
            //SetFormEditEnable();

            var saveUdiDataReq = new PDAXCSTOCKOPRequest()
            {
                OrgID = GlobalProxySetting.OrgID,
                EmpCode = GlobalProxySetting.GetLoginState().EmployeeCode,// GlobalProxySetting.UserID,//KB023 2023-12-26 关联员工报错
                OPType = 12,
                cfher2 = XCSTOCKMainInfo.chker2,
                cbilid = XCSTOCKMainInfo.cbilid,
                LRInfoTrace12List = CurrentEditTrace12List,
                LRInfoTrace13List = CurrentEditTrace13List,
                LRInfoList = resultList,
                cbiltype = strcbiltype
            };

            var saveUdiRes = this.Proxy.Execute(saveUdiDataReq);
            if (saveUdiRes != null && saveUdiRes.IsError)
            {
                Toast.MakeText(this, "清空出库复核临时保存数据出错！" + saveUdiRes.ErrorMessage, 0).Show();
                return;
            }
        }
        public void OnDismiss()
        {
            //throw new NotImplementedException();
        }

        public void OnItemClick(AdapterView parent, View view, int position, long id)
        {

            switch (mtype)
            {
                case 1:
                    txtcph.Text = listcph[position].ToString();
                    var list1 = resultList.Where(a => (a.cph == txtcph.Text) && (a.iflag < 100)).ToList();
                    if (list1 != null && list1.Count > 0)
                    {
                        SetFormText(list1[0]);
                        foreach (var item in resultList)
                        {
                            if (item.id1 == list1[0].id1)
                            {
                                seldialogId1 = item.id1;
                            }
                        }
                    }
                    else
                    {
                        seldialogId1 = -1;
                    }
                    break;
                case 2:
                    spinnerPopcfileno.Dismiss();
                    if (cfilenogoodfilelist != null && cfilenogoodfilelist.Count > 0)
                    {
                        txtFILE.Text = cfilenogoodfilelist[position].ToString();
                        //cfilenogoodfilelist = null;
                        mtype = 0;
                    }
                    break;
                case 3:
                    popUDIWindowAdapter.Dismiss();
                    DataRow FindSel = phpopWindowList[position];
                    XCSTOCKGoods tmpXCGoods = null;

                    tmpXCGoods = DataTableHelper.DataRowToClass<XCSTOCKGoods>(FindSel);

                    if (tmpXCGoods != null)
                    {
                        //如果已经存在已确认的品种批号，则需要赋值id1
                        if (resultList != null && resultList.Count > 0 && iSamePHProcess > 1 && resultList.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph.Contains(tmpXCGoods.cph) && a.dmadedate == tmpXCGoods.dmadedate && a.dexpdate == tmpXCGoods.dexpdate && a.id1 != 0).Any())
                        {
                            var tmpresult = resultList.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph.Contains(tmpXCGoods.cph) && a.dmadedate == tmpXCGoods.dmadedate && a.dexpdate == tmpXCGoods.dexpdate && a.id1 != 0).ToList();

                            if (GlobalDataCache.p_SOFTUSERTYPE == "zz579" && strcbiltype == "CC" && bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)//   KB023 2024-03-28  
                            {
                                var tmpresult2 = resultList.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph.Contains(tmpXCGoods.cph) && a.dmadedate == tmpXCGoods.dmadedate && a.dexpdate == tmpXCGoods.dexpdate && a.creserve1 == ChooseGoodsInfo[0].cudicode && a.id1 != 0).ToList();
                                if (tmpresult2 != null && tmpresult2.Count > 0)
                                {
                                    tmpresult = tmpresult2;
                                }
                            }

                            seldialogId1 = tmpresult[0].id1;
                            tmpXCGoods.ffhqty = tmpresult[0].ffhqty;//复核数量
                            tmpXCGoods.id1 = seldialogId1;
                            selDelId1 = seldialogId1;
                            /*
                             * FindViewById<TextView>(Resource.Id.txtscrq).Text = info.dmadedate;
                    FindViewById<TextView>(Resource.Id.txtyxrq).Text = info.dexpdate;
                             */
                            if (FindViewById<TextView>(Resource.Id.txtcgoodsname).Text != tmpXCGoods.cgoodsname || !tmpXCGoods.cph.Contains(FindViewById<TextView>(Resource.Id.txtcph).Text) || tmpXCGoods.dmadedate != FindViewById<TextView>(Resource.Id.txtscrq).Text || tmpXCGoods.dexpdate != FindViewById<TextView>(Resource.Id.txtyxrq).Text)
                            {
                                SetNewFormText(tmpXCGoods);
                            }
                        }
                        else if (resultList != null && resultList.Count > 0 && iSamePHProcess <= 1 && resultList.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph == tmpXCGoods.cph && a.id1 != 0).Any())
                        {
                            var tmpresult = resultList.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph == tmpXCGoods.cph && a.id1 != 0).ToList();

                            if (GlobalDataCache.p_SOFTUSERTYPE == "zz579" && strcbiltype == "CC" && bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)//   KB023 2024-03-28  
                            {
                                var tmpresult2 = resultList.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph == tmpXCGoods.cph && a.creserve1 == ChooseGoodsInfo[0].cudicode && a.id1 != 0).ToList();
                                if (tmpresult2 != null && tmpresult2.Count > 0)
                                {
                                    tmpresult = tmpresult2;
                                }
                            }

                            seldialogId1 = tmpresult[0].id1;
                            tmpXCGoods.ffhqty = tmpresult[0].ffhqty;//复核数量
                            tmpXCGoods.id1 = seldialogId1;
                            selDelId1 = seldialogId1;
                            if (FindViewById<TextView>(Resource.Id.txtcgoodsname).Text != tmpXCGoods.cgoodsname && FindViewById<TextView>(Resource.Id.txtcph).Text != tmpXCGoods.cph)
                            {
                                SetNewFormText(tmpXCGoods);
                            }
                        }
                        //更新数量 
                        var bresult = false;

                        var tmp = CoumTracefqty(tmpXCGoods, "check", ref bresult);

                        if (tmp + tmpXCGoods.finputqty > tmpXCGoods.fnoticeqty)
                        {
                            Toast.MakeText(this, "出库复核数量[" + tmp.ToString() + "]+复核异常数量[" + ConvertHelper.ToDecimal(tmpXCGoods.finputqty).ToString() + "]不能大于通知数量数量[" + tmpXCGoods.fnoticeqty.ToString() + "]！", 0).Show();
                            isPopuChooseForm = false;
                            return;
                        }
                        tmpXCGoods.ffhqty = tmp;
                        //tmpXCGoods.fnormvalue = ConvertHelper.ToDecimal(tmp) * ConvertHelper.ToDecimal(txtprice.Text);
                        if (!bresult)
                        {
                            AddCurrentEditTraceList(ref tmpXCGoods);
                        }
                        // 判断是否存在序列号
                        if (CurrentEditTrace12List != null && iSamePHProcess <= 0 && CurrentEditTrace12List.Count > 0 && CurrentEditTrace12List.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph == tmpXCGoods.cph).Any())
                        {
                            // tmp = tmp;
                        }
                        else if (iSamePHProcess > 1 && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && resultList.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph == tmpXCGoods.cph && a.dmadedate == tmpXCGoods.dmadedate && a.dexpdate == tmpXCGoods.dexpdate && a.ref_cbilid == tmpXCGoods.ref_cbilid && a.id1 == tmpXCGoods.id1).Any())
                        {
                            var tmpresult = resultList.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph == tmpXCGoods.cph && a.dmadedate == tmpXCGoods.dmadedate && a.dexpdate == tmpXCGoods.dexpdate && a.ref_cbilid == tmpXCGoods.ref_cbilid && a.id1 == tmpXCGoods.id1).FirstOrDefault();
                            if (tmpresult != null)
                            {
                                tmp = tmpresult.ffhqty + ChooseGoodsInfo[0].fqty;
                            }
                            else
                            {
                                tmp = ConvertHelper.ToDecimal(this.txtfqty.Text) + ChooseGoodsInfo[0].fqty;
                            }
                        }
                        else if (iSamePHProcess <= 1 && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && resultList.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph == tmpXCGoods.cph && a.ref_cbilid == tmpXCGoods.ref_cbilid && a.id1 == tmpXCGoods.id1).Any())
                        {
                            var tmpresult = resultList.Where(a => a.cgoodsid == tmpXCGoods.cgoodsid && a.cph == tmpXCGoods.cph && a.ref_cbilid == tmpXCGoods.ref_cbilid && a.id1 == tmpXCGoods.id1).FirstOrDefault();
                            if (tmpresult != null)
                            {
                                tmp = tmpresult.ffhqty + ChooseGoodsInfo[0].fqty;
                            }
                            else
                            {
                                tmp = ConvertHelper.ToDecimal(this.txtfqty.Text) + ChooseGoodsInfo[0].fqty;
                            }
                        }
                        else
                        {
                            //如果没有扫码，则

                            tmp = ConvertHelper.ToDecimal(this.txtfqty.Text) + ChooseGoodsInfo[0].fqty;
                        }
                        tmpXCGoods.ffhqty = tmp;
                        if (!bresult)
                        {
                            //this.txtfqty.Text = tmp.ToString(GlobalDataCache.QtyFormat);
                            //this.txtvalue.Text = (ConvertHelper.ToDecimal(txtfqty.Text) * ConvertHelper.ToDecimal(txtprice.Text)).ToString(GlobalDataCache.ValueFormat);
                            GlobalDataCache.SetData("PDACKFHDGoodsInfo", tmpXCGoods);
                        }
                        SetNewFormText(tmpXCGoods);
                        if (tmpXCGoods != null)
                        {
                            for (int i = 0; i < resultList.Count; i++)
                            {
                                if (resultList[i].id1 == tmpXCGoods.id1)
                                {
                                    resultList[i].ffhqty = tmpXCGoods.ffhqty;
                                    break;
                                }
                            }
                            _originalAdapter.UpdateData(resultList);
                        }
                        this.txtInput.Focusable = true;     //定位到品种扫描
                        this.txtInput.FocusableInTouchMode = true;
                        this.txtInput.RequestFocus();
                    }
                    isPopuChooseForm = false;
                    mtype = 0;
                    break;
                default:
                    if (dialogUDIList != null && dialogUDIList.Count > 1 && ChooseGoodsInfo != null && ChooseGoodsInfo.Count < 1)
                    {
                        popUDIWindowAdapter.Dismiss();
                        DataRow UDISel = dialogUDIList[position];
                        if (bchkUDI)
                        {
                            InstrumentGoodsInfo goodsInfo = null;
                            goodsInfo = DataTableHelper.DataRowToClass<InstrumentGoodsInfo>(UDISel);
                            ChooseGoodsInfo.Add(goodsInfo);
                            txtInput.Text = ChooseGoodsInfo[0].cgoodsid;
                            _Rulecode = goodsInfo.crulecode;
                            _LastUDIgoodsid = goodsInfo.cgoodsid;
                            txtQuery_Click(null, null);
                        }
                        dialogUDIList = null;
                    }
                    else if (dialogQRList != null && dialogQRList.Count > 1)
                    {
                        popUDIWindowAdapter.Dismiss();
                        DataRow UDISel = dialogQRList[position];
                        infoQr = new QRDealGoodsData();
                        infoQr = DataTableHelper.DataRowToClass<QRDealGoodsData>(UDISel);
                        txtInput.Text = infoQr.cgoodsid;
                        txtQuery_Click(null, null);
                        dialogQRList = null;
                    }
                    break;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _wrappedAdapter?.Dispose();
        }
        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Android.App.Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == 180)
            {
                loadform();
            }

            if (resultCode == Android.App.Result.Ok)
            {
                if (requestCode == 100)//复核确认
                {
                    //获取复核数据
                    MainGoodsInfo = GlobalDataCache.GetData<XCSTOCKGoods>("CheckFHDetailGoods");
                    for (int i = 0; i < resultList.Count; i++)
                    {
                        if (resultList[i].id1 == MainGoodsInfo.id1)
                        {
                            resultList[i].cph = MainGoodsInfo.cph;
                            resultList[i].dmadedate = MainGoodsInfo.dmadedate;
                            resultList[i].dexpdate = MainGoodsInfo.dexpdate;
                            resultList[i].ffhqty = MainGoodsInfo.ffhqty;
                            //resultList[i].finputqty = MainGoodsInfo.finputqty;
                            resultList[i].fcancelqty = MainGoodsInfo.fcancelqty;
                            resultList[i].ccancelseason = MainGoodsInfo.ccancelseason;
                            if (resultList[i].ffhqty == resultList[i].fnoticeqty)
                            {
                                resultList[i].finputqty = 0;
                            }
                            break;
                        }
                    }
                    _originalAdapter.UpdateData(resultList);
                    ClearFormText();
                }
                if (requestCode == 101)//异常复核
                {
                    var resultist = GlobalDataCache.GetData<List<XCSTOCKGoods>>("CheckYCFHDetailGoodsList");
                    for (int i = 0; i < resultList.Count; i++)
                    {
                        for (int j = 0; j < resultist.Count; j++)
                        {
                            if (resultist[j].id1 == resultList[i].id1 && resultist[j].finputqty > 0)
                            {
                                resultList[i].finputqty = resultist[j].finputqty;
                                break;
                            }
                        }
                    }
                    _originalAdapter.UpdateData(resultList);
                    ClearFormText();
                }
            }
        }
    }

    #region listview适配器
    class CKFHDGoodsInfoAdapter : BaseRefreshableAdapter<XCSTOCKGoods>
    {
        LayoutInflater inflater;
        //List<XCSTOCKGoods> items;
        public CKFHDGoodsInfoAdapter(Activity context, List<XCSTOCKGoods> items) : base(context, items)
        {
            this.inflater = LayoutInflater.From(context);
            //this.items = items;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = this.Items[position];
            CKFHDGoodsInfoViewHolder viewHolder = null;
            if (convertView == null)
            {
                viewHolder = new CKFHDGoodsInfoViewHolder();
                convertView = inflater.Inflate(Resource.Layout.xcstocklrlist, parent, false);
                convertView.DrawingCacheEnabled = true;
                convertView.DrawingCacheQuality = DrawingCacheQuality.High;
                viewHolder.textView1 = convertView.FindViewById<TextView>(Resource.Id.textView1);
                viewHolder.textView2 = convertView.FindViewById<TextView>(Resource.Id.textView2);
                viewHolder.textView3 = convertView.FindViewById<TextView>(Resource.Id.textView3);
                viewHolder.textView4 = convertView.FindViewById<TextView>(Resource.Id.textView4);
                convertView.Tag = viewHolder;
            }
            else
            {
                viewHolder = convertView.Tag as CKFHDGoodsInfoViewHolder;
            }
            if (GlobalDataCache.GetData<XCSTOCKGoods>("PDACKFHDGoodsInfo") == item)
            {
                //convertView.SetBackgroundResource(Resource.Drawable.rectangle);
                //convertView.SetBackgroundColor((convertView.Context as BaseFrom).custom_blue);
            }
            else
            {
                //convertView.SetBackgroundResource(0);
            }
            viewHolder.Data = item;
            viewHolder.textView1.Text = item.ccommonname.ToString();
            viewHolder.textView2.Text = item.cph.ToString();
            viewHolder.textView3.Text = item.fnoticeqty.ToString(GlobalDataCache.QtyFormat);
            viewHolder.textView4.Text = item.ffhqty.ToString(GlobalDataCache.QtyFormat);
            var ccolor = (convertView.Context as BaseFrom).custom_title;
            if (item.finputqty > 0)
            {
                ccolor = (convertView.Context as BaseFrom).custom_yellow;
            }
            else if (item.ffhqty > 0)
            {
                ccolor = (convertView.Context as BaseFrom).custom_red;
            }

            viewHolder.textView1.SetTextColor(ccolor);
            viewHolder.textView2.SetTextColor(ccolor);
            viewHolder.textView3.SetTextColor(ccolor);
            viewHolder.textView4.SetTextColor(ccolor);
            return convertView;
        }

    }

    class CKFHDGoodsInfoViewHolder : Java.Lang.Object
    {
        public TextView textView1;
        public TextView textView2;
        public TextView textView3;
        public TextView textView4;
        public XCSTOCKGoods Data { get; set; }

    }
    #endregion
}