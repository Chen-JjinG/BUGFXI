using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using K9PDA.Infrastructure.Model;
using K9PDA.Infrastructure.Request;
using K9PDA.Infrastructure.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using static Android.App.ActionBar;
using static Android.Widget.AdapterView;
using System.Data;
using K9PDA.Adapter;
using ZXing.Mobile;
using System.Threading.Tasks;
using Android.Views.Animations;
using K9PDA.Customized.UIGenerateHelper;
using K9PDA.Infrastructure;
using Android.Runtime;
using K9PDA.Infrastructure.Response;

/* ------------------------------------------------
版本记录      版本日期      编辑人      编辑内容
V1.2          2025-08-05    CJJ       BUG#59662增加显示生产许可证
V1.3          2025-08-07    CJJ       PDA 增加UDI采码相关设置（主要是批量采集UDI）
V1.4          2025-08-07    CJJ       对抗屎山代码写的临时表
--------------------------------------------------- */

namespace K9PDA
{
    [Activity(Label = "shlrform", ParentActivity = typeof(shform))]
    //这里有个待优化的内容，但是改动比较大
    //原先设计时是在这个页面会有保存操作
    //但是这个保存操作和后面设计的新采码流程存在一定冲突
    //所以会要求在采码前调用一次保存
    //导致按钮响应时间较长,可以考虑先找个地方缓存数据，但是改动有点大
    //上面这段话20250528写的，接口内容比较多，待后续优化
    public class shlrform : BaseFrom, PopupWindow.IOnDismissListener, IOnItemClickListener
    {

        #region Property

        private EditText txtPH;
        private EditText txtSHSL;
        private EditText txtSCDATE;
        private EditText txtYXDATE;
        private EditText txtSXDATE;
        private TextView txtGOODSNAME;
        private TextView txtFACTORYNAME;
        private EditText txtFILE;
        private TextView txtBARCODE;
        private TextView lblDW;
        private EditText txtINPUT;
        private TextView txtQSSL;
        private Spinner spSeason;
        private ListView listView;

        #region CJJ 20250510 BUG#59785 增加灭菌批号,灭菌效期，灭菌日期

        private TextView txt_mjph;//灭菌批号
        private TextView txt_mjrq;//灭菌日期
        private TextView txt_mjxq;//灭菌效期

        #endregion

        private LinearLayout linearlayout_info;
        private Button btnCollapse;
        private Button btn_code_scan;
        private CheckBox fz_chk_sel;
        private Button btnNewPH;
        private Button btnDelPH;
        private TextView txtGG;
        private TextView txt_hw;
        private TextView txt_notice_fqty;
        private TextView txt_refcbilid;
        private TextView txt_scxkz;
        private ListView listview;
        private List<string> rejectionReasons;  //拒收原因

        private SHMainInfo CurrentSHMainInfo;       //当前收货单主表信息
        private SHGoods CurrentEditGoods;           //当前编辑商品明细
        private SHPHInfo CurrentEditGoodPHInfo;//当前编辑品种批号信息
        public List<SHGoods> CurrentEditGoodsList;//当前操作的商品明细记录数据
        private List<SHPHInfo> CurrentEditGoodPHInfoList;  //当前品种批号列表，删除批号时不会删除该列表
        private ColdStoreInfo ColdStore;

        private ListViewPopWindowAdapter popWindowAdapter;
        private List<SHGoods> dialogList;

        private List<PDAGoodscfilenofilelistRequest> cfilenofilelist;//商品注册证选择
        private List<string> cfilenogoodfilelist;//商品注册证选择
        private SpinnerPopWindowAdapter<string> spinnerPopcfileno;
        private int mtype = 0;
        QRDealGoodsData infoQr = new QRDealGoodsData();
        private List<DataRow> dialogQRList;
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
        private bool IsEnableSFDARenewal;
        private string _lastCode { get; set; }
        private string UDIlastCode;
        private int _lastUDIRuleLen { get; set; } //解析为UDI码规则的条码总长度
        private int UDIastUDIRuleLen;
        private string _Rulecode { get; set; } //规则编码
        private string _LastUDIgoodsid { get; set; }//前一个扫描选择的商品id
        private string UDIRulecode;

        private int scanCount;//扫描次数 V1.3

        private Dictionary<string, decimal> tempRefQty;//V1.4

        #endregion
        private string strpdashGetddtype;
        bool isPopuChooseForm = false;
        private int iSamePHProcess = 0;
        private bool IsUseNewScanFlow;//是否启用新采码流程

        //拍照
        private bool bMobileScan;
        MobileBarcodeScanner scanner;
        View zxingOverlay;

        private UDIScanSettingHelper udiScanSettingHelper;
        #endregion

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetTheme(Android.Resource.Style.ThemeLightNoTitleBar);
            this.Window.SetSoftInputMode(SoftInput.StateAlwaysHidden);
            SetContentView(Resource.Layout.shlrform);
            tempRefQty = new Dictionary<string, decimal>();//V1.4
            CurrentSHMainInfo = GlobalDataCache.GetData<SHMainInfo>("PDASHSelectInfo");
            ImageView ac = FindViewById<ImageView>(Resource.Id.actionMenuView1);
            ac.Click += TB_NavigationOnClick;
            GlobalDataCache.SetData("REFRESHSHEditGoodsList", new Action(() => { this.RefreshCurrentEditGoodsList(); }));

            this.txtINPUT = FindViewById<EditText>(Resource.Id.txtgoods);
            //txtINPUT.SetBackgroundResource(Resource.Drawable.custom_spinner_background_search);
            //SetTextDraw(txtINPUT, Resource.Drawable.search_new, 0, 0, 7, 7);//添加搜索图标
            this.txtGOODSNAME = FindViewById<TextView>(Resource.Id.txtcgoodsname);
            this.lblDW = FindViewById<TextView>(Resource.Id.txtdw);
            this.txtGG = FindViewById<TextView>(Resource.Id.txtcpkname);
            this.txtBARCODE = FindViewById<TextView>(Resource.Id.txtbarcode);
            this.txtFILE = FindViewById<EditText>(Resource.Id.txtcfile);
            txtFILE.SetBackgroundResource(Resource.Drawable.custom_spinner_background);
            //SetTextDraw(txtFILE, Resource.Drawable.expand, 0, 0, 7, 7);//添加下拉图标

            #region CJJ 20250510 BUG#59785 增加灭菌批号,灭菌效期，灭菌日期

            //this.txt_mjph = FindViewById<EditText>(Resource.Id.txt_mjph);//灭菌批号
            //this.txt_mjrq = FindViewById<EditText>(Resource.Id.txt_mjrq);//灭菌日期
            //this.txt_mjxq = FindViewById<EditText>(Resource.Id.txt_mjxq);//灭菌效期

            #endregion

            this.txt_notice_fqty = FindViewById<TextView>(Resource.Id.txt_notice_fqty);
            this.txtFACTORYNAME = FindViewById<TextView>(Resource.Id.txthw);
            this.txtPH = FindViewById<EditText>(Resource.Id.txt_ph);
            this.txtSCDATE = FindViewById<EditText>(Resource.Id.txt_scrq);
            this.txtSHSL = FindViewById<EditText>(Resource.Id.txt_pdsl);
            this.txtYXDATE = FindViewById<EditText>(Resource.Id.txt_yxq);
            this.txtSXDATE = FindViewById<EditText>(Resource.Id.txt_sxq);
            this.txtQSSL = FindViewById<EditText>(Resource.Id.txt_qssl);
            this.spSeason = FindViewById<Spinner>(Resource.Id.spinner1);
            this.txt_hw = FindViewById<TextView>(Resource.Id.txt_hw); //货位
            this.txt_refcbilid = FindViewById<TextView>(Resource.Id.txt_refcbilid);//源单号
            this.txt_scxkz = FindViewById<TextView>(Resource.Id.txt_scxkz);
            btnNewPH = FindViewById<Button>(Resource.Id.btn_new);//新建批号
            btnDelPH = FindViewById<Button>(Resource.Id.btn_cancel);//删除批号
            Button btn_YS = FindViewById<Button>(Resource.Id.btn_ys);//已收
            Button btn_ZF = FindViewById<Button>(Resource.Id.btn_zf);//作废
            Button btn_BC = FindViewById<Button>(Resource.Id.btn_tj);//保存
            Button btn_LC = FindViewById<Button>(Resource.Id.btn_lc);//冷藏记录

            Button btn_code_scan = FindViewById<Button>(Resource.Id.btn_code_scan);//采码按钮
            btn_code_scan.Click += Btn_code_scan_Click;
            IsUseNewScanFlow = GlobalProxySetting.ConfigList.Any(a => a.cparmname == "UseZsmNewProcess" && a.cparmvalue == "1");
            if (!IsUseNewScanFlow)
            {
                btn_code_scan.Visibility = ViewStates.Gone;
            }

            this.listView = FindViewById<ListView>(Resource.Id.listView1);

            #region 组合条码
            //扫码相关选项
            var linearLayoout_option = FindViewById<LinearLayout>(Resource.Id.linearLayoout_option);
            fz_chk_sel = FindViewById<CheckBox>(Resource.Id.fz_chk_sel); //辅助条码
            this.chkUDI = FindViewById<CheckBox>(Resource.Id.checkUDI);//是否扫组合条码

            //V1.3
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

            if (IsUseNewScanFlow)
            {
                udiScanSettingHelper = new UDIScanSettingHelper(this, linearLayoout_option);
            }

            if (GlobalProxySetting.ConfigList != null && GlobalProxySetting.ConfigList.Where(a => (a.cparmname == "INSTRCODE") && (a.cparmvalue == "1")).Any())//是否启用UDI组合条码
            {

                chkUDI.Visibility = ViewStates.Visible;
                CurrentEditTrace12List = new List<GoodsTrace12>();
                CurrentEditTrace13List = new List<GoodsTrace13>();

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
            if (GlobalProxySetting.ConfigList != null && GlobalProxySetting.ConfigList.Where(a => a.cparmname == "PDASHGETDDTYPE").Any())
            {
                strpdashGetddtype = GlobalProxySetting.ConfigList.First(c => c.cparmname == "PDASHGETDDTYPE").cparmvalue;// == "2";//每次选择
            }


            #endregion
            IsEnableSFDARenewal = GlobalProxySetting.ConfigList != null && GlobalProxySetting.ConfigList.Where(a => (a.cparmname == "IsEnableSFDARenewal") && (a.cparmvalue == "1")).Any();
            if (GlobalProxySetting.ConfigList != null && GlobalProxySetting.ConfigList.Where(a => a.cparmname == "SamePHProcess").Any())
            {
                iSamePHProcess = ConvertHelper.ToInt(GlobalProxySetting.ConfigList.First(c => c.cparmname == "SamePHProcess").cparmvalue);//2024-06-15 出现相同批号，不同产期或有效期时处理
            }
            #region 拍照
            var btnmobilescan = FindViewById<ImageButton>(Resource.Id.btnphonescan);
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

            #region 详情信息折叠

            linearlayout_info = FindViewById<LinearLayout>(Resource.Id.linearlayout_info);
            btnCollapse = FindViewById<Button>(Resource.Id.btn_collapse);
            btnCollapse.Click += BtnCollapse_Click;

            #endregion

            if (GlobalDataCache.P_productTRADE == "器械")
            {
                FindViewById<TextView>(Resource.Id.txtviewcfile).Text = "注册证号：";
            }
            listView.ChildViewAdded -= ListView_ChildViewAdded;
            listView.ChildViewAdded += ListView_ChildViewAdded;
            listView.ItemClick += ListView_ItemClick;
            listView.ItemsCanFocus = true;

            var tv_cgoodsid = FindViewById<TextView>(Resource.Id.tv_cgoodsid);
            FindViewById<LinearLayout>(Resource.Id.ll_wms).Visibility = GlobalDataCache.EnableWMS ? ViewStates.Visible : ViewStates.Gone;
            tv_cgoodsid.Visibility = GlobalDataCache.EnableWMS ? ViewStates.Visible : ViewStates.Gone;
            tv_cgoodsid.Click += Tv_cgoodsid_Click;

            spSeasonInit();
            spSeason.ItemSelected += spSeason_ItemSelected;

            this.txtSCDATE.InputType = InputTypes.ClassNumber;
            this.txtYXDATE.InputType = InputTypes.ClassNumber;
            this.txtSXDATE.InputType = InputTypes.ClassNumber;

            this.txtSCDATE.Text = "";
            this.txtYXDATE.Text = "";
            this.txtSXDATE.Text = "";
            txtSCDATE.Focusable = true;
            txtSCDATE.FocusableInTouchMode = true;
            txtYXDATE.Focusable = true;
            txtYXDATE.FocusableInTouchMode = true;
            txtSXDATE.Focusable = true;
            txtSXDATE.FocusableInTouchMode = true;

            this.txtINPUT.KeyPress -= txtINPUT_KeyPress;
            this.txtINPUT.KeyPress += txtINPUT_KeyPress;
            this.txtPH.KeyPress -= txtPH_KeyPress;
            this.txtPH.KeyPress += txtPH_KeyPress;

            this.txtSCDATE.KeyPress -= Scdate_KeyPress;
            this.txtYXDATE.KeyPress -= YXdate_KeyPress;
            this.txtSXDATE.KeyPress -= SXdate_KeyPress;
            this.txtSCDATE.KeyPress += Scdate_KeyPress;
            this.txtYXDATE.KeyPress += YXdate_KeyPress;
            this.txtSXDATE.KeyPress += SXdate_KeyPress;
            this.txtSHSL.KeyPress -= txtSHSL_KeyPress;
            this.txtSHSL.KeyPress += txtSHSL_KeyPress;
            this.txtSHSL.FocusChange -= txtSHSL_FocusChange;
            this.txtSHSL.FocusChange += txtSHSL_FocusChange;
            this.txtSCDATE.FocusChange += TxtSCDATE_FocusChange;
            this.txtYXDATE.FocusChange += TxtYXDATE_FocusChange;
            this.txtSXDATE.FocusChange += TxtSXDATE_FocusChange;

            btnNewPH.Click += BtnNewPH_Click;
            btnDelPH.Click += BtnDelPH_Click;
            btn_YS.Click += Btn_YS_Click;
            btn_ZF.Click += Btn_ZF_Click;
            btn_BC.Click += Btn_BC_Click;
            btn_LC.Click += Btn_LC_Click;

            CurrentEditGoodsList = new List<SHGoods>();
            ColdStore = new ColdStoreInfo();

            #region CJJ 20250228 为当前页面实现一些统一的操作定义

            TextViewToastTextHelper.ApplyGlobalClick(this);
            var selectorHelper = new ListViewColorHelper<SHLRGoodsInfoAdapter>(listView);

            #endregion


            InitCurrentEditGoodsList();

            GC.Collect();
        }

        private void txtINPUT_KeyPress(object sender, View.KeyEventArgs e)
        {
            txtINPUT_KeyPressAsync(sender, e);
        }

        #region 控件事件

        /// <summary>
        /// 折叠展开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCollapse_Click(object sender, EventArgs e)
        {
            LinearLayoutCollapseHelper.CollapseExpand(linearlayout_info, btnCollapse);
        }

        #region Btn_code_scan ScanCode 新流程采码

        /// <summary>
        /// V1.3
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_code_scan_Click(object sender, EventArgs e)
        {
            if (this.btnNewPH.Text == "完成")//2023-12-11
            {
                Toast.MakeText(this, "请先点击完成按钮后才能进行保存操作", 0).Show();
                return;
            }

            ScanCode();

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

        private async void ScanCode(bool isInvokeFromSave = false)
        {
            var dataDic = ConvertHelper.ConvertObjToDic(CurrentSHMainInfo);
            if (!dataDic.ContainsKey("ctablename"))
            {
                dataDic.Add("ctablename", "bl_shd");
            }
            //var response = new PDAWMSSHOPResponse();


            if (!isInvokeFromSave)
            {
                if (CurrentSHMainInfo == null || string.IsNullOrEmpty(CurrentSHMainInfo.cbilid)
    || CurrentEditGoodsList == null || CurrentEditGoodsList.Count <= 0)
                {
                    Toast.MakeText(this, "采码前保存失败！保存数据不能为空。", 0).Show();
                    return;
                }

                await this.Proxy.ExecuteAsync(new PDASHOPRequest()
                {
                    OPType = 7,
                    OrgID = GlobalProxySetting.OrgID,
                    EmpCode = GlobalProxySetting.GetLoginState().EmployeeCode,
                    SHInfo = CurrentSHMainInfo,
                    LRInfoList = CurrentEditGoodsList,
                    ColdStore = ColdStore,
                    LRInfoTrace12List = CurrentEditTrace12List,
                    LRInfoTrace13List = CurrentEditTrace13List
                }, response =>
                {
                    if (response == null)
                    {
                        this.RunOnUiThread(() => Toast.MakeText(this, "采码前保存失败！", 0).Show());
                        return;
                    }

                    if (response.IsError && response.ErrorMessage != "isNotFinishScanYet")
                    {
                        this.RunOnUiThread(() => Toast.MakeText(this, "采码前保存失败:" + response.ErrorMessage, 0).Show());
                        return;
                    }

                    if (response.ResponseCode != null && !string.IsNullOrEmpty(response.ResponseCode))
                    {
                        dataDic["cbilid"] = response.ResponseCode;
                        GlobalDataCache.SetData("temp_sh_cbilid", response.ResponseCode);
                        CurrentSHMainInfo.cbilid = response.ResponseCode;
                    }
                }, this);



            }

            var request = new BusinessRequest() { BusinessKey = "PDAScanCodeProcess" };
            request.Parameters["opType"] = 1;
            request.Parameters["cempid"] = GlobalProxySetting.GetLoginState().EmployeeCode;
            request.Parameters["tableName"] = dataDic["ctablename"].ToString();

            if (dataDic.ContainsKey("cbilid") && !string.IsNullOrEmpty(ConvertHelper.ToString(dataDic["cbilid"])))
            {
                request.Parameters["cbilid"] = dataDic["cbilid"].ToString();
            }
            else
            {
                request.Parameters["cbilid"] = GlobalDataCache.GetData<string>("temp_sh_cbilid");
            }


            request.Parameters["queryText"] = string.Empty;

            var res = this.Proxy.Execute(request);

            if (res.IsError)
            {
                Toast.MakeText(this, res.ErrorMessage, 0).Show();
                return;
            }

            if (!res.Result.ContainsKey("dt") || (res.Result["dt"] as DataTable) == null || (res.Result["dt"] as DataTable).Select().Length <= 0)
            {
                if (isInvokeFromSave)
                {
                    Toast.MakeText(this, "保存成功", 0).Show();
                    TB_NavigationOnClick(null, null);
                    GlobalDataCache.GetData<Action>("REFRESHSHLIST")();
                    return;
                }
                else
                {
                    Toast.MakeText(this, "不存在需要采码的商品明细！", 0).Show();
                    return;
                }
            }

            ClearEditData();
            StartTraceCodeScan.Start(dataDic, GlobalProxySetting, this);

        }

        #endregion

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

                        txtINPUT.Text = result.Text;
                        View.KeyEventArgs keys = new View.KeyEventArgs(true, Keycode.Enter, new KeyEvent(KeyEventActions.Up, Keycode.Enter));
                        txtINPUT_KeyPressAsync(null, keys);
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


                    this.txtINPUT.Focusable = true;
                    this.txtINPUT.FocusableInTouchMode = true;
                    this.txtINPUT.RequestFocus();
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

        private void Tv_cgoodsid_Click(object sender, EventArgs e)
        {
            var goodsedit = JSONSerializer.Deserialize<PDAGoodsEdit>(JSONSerializer.Serialize(CurrentEditGoods));
            GlobalDataCache.SetData("PDAGoodsEdit", goodsedit);
            Intent intent = new Intent(this, typeof(goodsedit));
            StartActivity(intent);
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
                custom.AlertDialogShow(this, "确定返回收货列表？");
                return true;
            }
            if (keyCode == Keycode.VolumeDown) //手机声音加键触发
            {
                txtINPUT.Text = "";
                txtINPUT.Focusable = true;
                txtINPUT.FocusableInTouchMode = true;
                txtINPUT.RequestFocus();
                scanner = new MobileBarcodeScanner();
                Task t = new Task(AutoScan);
                t.Start();
                return true;
            }


            if (keyCode == Keycode.VolumeUp) //手机声音减键触发
            {
                txtINPUT.Text = "";
                txtINPUT.Focusable = true;
                txtINPUT.FocusableInTouchMode = true;
                txtINPUT.RequestFocus();
                scanner = new MobileBarcodeScanner();
                Task t = new Task(AutoScan);
                t.Start();
                return true;
            }
            return base.OnKeyDown(keyCode, e);
        }
        private void TxtYXDATE_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            this.txtYXDATE.Text = ConvertHelper.ToDatetTimeString(this.txtYXDATE.Text.ToString().Trim());
            GetFILETEXT();
        }

        private void TxtSXDATE_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            this.txtSXDATE.Text = ConvertHelper.ToDatetTimeString(this.txtSXDATE.Text.ToString().Trim());
            GetFILETEXT();
        }

        private void TxtSCDATE_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            this.txtSCDATE.Text = ConvertHelper.ToDatetTimeString(this.txtSCDATE.Text.ToString().Trim());
        }

        private void Btn_LC_Click(object sender, EventArgs e)
        {
            if (CurrentEditGoodsList.Where(p => p.isgspcold == 1).ToList().Count() == 0)
            {
                Toast.MakeText(this, "该单不存在冷藏药品，无需做冷藏记录!", ToastLength.Short).Show();
                return;
            }
            Intent intent = new Intent(this, typeof(shcoldform));
            intent.PutExtra("coldcorpname", CurrentSHMainInfo.ccorpname);
            intent.PutExtra("coldaddress", CurrentSHMainInfo.caddress);
            intent.PutExtra("coldstoreformtype", "0");
            StartActivity(intent);
        }

        private void Btn_BC_Click(object sender, EventArgs e)
        {
            if (this.btnNewPH.Text == "完成")//2023-12-11
            {
                Toast.MakeText(this, "请先点击完成按钮后才能进行保存操作", 0).Show();
                return;
            }
            bool NotShData = true;
            if (CurrentEditGoodsList.Count > 0)
            {
                for (int i = 0; i < CurrentEditGoodsList.Count; i++)
                {
                    if (CurrentEditGoodsList[i].PHList.Count > 0)
                    {
                        NotShData = false;
                        break;
                    }
                }

            }
            if (NotShData)
            {
                Toast.MakeText(this, "收货明细不允许为空，请检查", 0).Show();
                return;
            }
            var findRows = CurrentEditGoodsList.Where(p => p.isgspcold == 1).ToList();//冷藏药品
            if (findRows.Count() > 0)
            {
                var errorList = new List<string>();
                foreach (var row in findRows)
                {
                    if (row.csendtype == null || row.csendtype == string.Empty || row.ctepctl == null || row.ctepctl == string.Empty)
                    {
                        errorList.Add(row.cgoodsid);
                    }
                }
                if (errorList.Count > 0)
                {
                    //返回禁止类
                    string errorMessage = string.Join(",", errorList.Select(r => "[" + r + "]")) + "为[冷藏药品],需要填写冷藏记录！";
                    Toast.MakeText(this, errorMessage, 0).Show();
                    return;
                }
            }
            CustomAlertDialog cadBC = new CustomAlertDialog();
            cadBC.OKClick += CadBC_OKClick;
            cadBC.AlertDialogShow(this, "确定提交收货单录入数据？");

        }

        private void CadBC_OKClick(object sender, EventArgs e)
        {
            if (CurrentSHMainInfo.cbilid == null || string.IsNullOrEmpty(CurrentSHMainInfo.cbilid))
            {
                CurrentSHMainInfo.cbilid = GlobalDataCache.GetData<string>("temp_sh_cbilid");
            }

            if (CurrentSHMainInfo == null || string.IsNullOrEmpty(CurrentSHMainInfo.cbilid)
                || CurrentEditGoodsList == null || CurrentEditGoodsList.Count <= 0)
            {
                Toast.MakeText(this, "保存数据不能为空！", 0).Show();
                return;
            }

            //this.OnDismiss();
            this.Proxy.Execute(new PDASHOPRequest()
            {
                OPType = 7,
                OrgID = GlobalProxySetting.OrgID,
                EmpCode = GlobalProxySetting.GetLoginState().EmployeeCode,
                SHInfo = CurrentSHMainInfo,
                LRInfoList = CurrentEditGoodsList,
                ColdStore = ColdStore,
                LRInfoTrace12List = CurrentEditTrace12List,
                LRInfoTrace13List = CurrentEditTrace13List
            }, (response) =>
            {
                if (!response.IsError)
                {
                    Toast.MakeText(this, "保存成功", 0).Show();
                    TB_NavigationOnClick(null, null);
                    GlobalDataCache.GetData<Action>("REFRESHSHLIST")();
                }
                else if (response.IsError && response.ErrorMessage == "isNotFinishScanYet")
                {
                    var temp_cbilid = (response.ResponseCode == null || string.IsNullOrEmpty(response.ResponseCode)) ? CurrentSHMainInfo.cbilid : response.ResponseCode;
                    GlobalDataCache.SetData("temp_sh_cbilid", temp_cbilid);
                    CurrentSHMainInfo.cbilid = temp_cbilid;
                    ScanCode(true);
                }
                else
                {
                    Toast.MakeText(this, "保存失败", 0).Show();
                }
            }, this);
        }

        private void Btn_YS_Click(object sender, EventArgs e)
        {
            GC.Collect();
            Intent intent = new Intent(this, typeof(shysform));
            intent.PutExtra("SH_ctype", "1");
            intent.PutExtra("SH_cbilid", CurrentSHMainInfo.cbilid);
            //GlobalDataCache.SetData("SH_CurrentEditTrace12List", CurrentEditTrace12List);
            //GlobalDataCache.SetData("SH_CurrentEditTrace13List", CurrentEditTrace13List);

            StartActivity(intent);
        }

        private void Btn_ZF_Click(object sender, EventArgs e)
        {
            CustomAlertDialog cadZF = new CustomAlertDialog();
            cadZF.OKClick += CadZF_OKClick;
            cadZF.AlertDialogShow(this, "确定作废当前收货录入数据？");
        }

        private void CadZF_OKClick(object sender, EventArgs e)
        {
            if (CurrentSHMainInfo.cbilid == "")
            {
                Toast.MakeText(this, "单据号不能为空！", 0).Show();
                return;
            }
            if (CurrentSHMainInfo.cbilid == "Add")
            {
                Toast.MakeText(this, "新增未保存单据不允许作废！", 0).Show();
                return;
            }
            this.Proxy.Execute(new PDASHOPRequest()
            {
                OPType = 6,
                EmpCode = GlobalProxySetting.GetLoginState().EmployeeCode,
                cbilid = CurrentSHMainInfo.cbilid
            }, (response) =>
            {
                if (!response.IsError)
                {
                    Toast.MakeText(this, "作废成功", 0).Show();
                    TB_NavigationOnClick(null, null);
                    GlobalDataCache.GetData<Action>("REFRESHSHLIST")();
                }
                else
                {
                    Toast.MakeText(this, "作废失败", 0).Show();
                }
            }, this);
        }

        private void txtSHSL_KeyPress(object sender, View.KeyEventArgs e)
        {
            e.Handled = false;
            if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Up)
            {
                this.btnNewPH.Focusable = true;
                this.btnNewPH.FocusableInTouchMode = true;
                this.btnNewPH.RequestFocus();

                this.txtSHSL.Focusable = true;
                this.txtSHSL.FocusableInTouchMode = true;
                this.txtSHSL.RequestFocus();
                txtSHSL.SelectAll();

                this.btnNewPH.FocusableInTouchMode = false;
                if (this.btnNewPH.Text == "完成")
                {
                    this.btnNewPH.Focusable = true;
                    this.btnNewPH.FocusableInTouchMode = true;
                    this.btnNewPH.RequestFocus();
                }
                else
                {
                    this.txtSHSL.SelectAll();
                    txtSHSL.SetSelectAllOnFocus(true);
                    txtSHSL.SelectAll();
                }
                e.Handled = true;


                GC.Collect();
            }
        }

        private void txtSHSL_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            if (this.btnNewPH.Text == "完成")
            {
                return;
            }
            if (this.CurrentEditGoods == null)
            {
                // Toast.MakeText(this, "扫描品种并选择批号！", 0).Show();
                return;
            }
            if (this.CurrentEditGoodPHInfo == null)
            {
                return;
            }
            if (!e.HasFocus)
            {
                decimal fshqty = 0;
                if (string.IsNullOrWhiteSpace(txtSHSL.Text))
                {
                    Toast.MakeText(this, "收货数量不能为空！", 0).Show();
                    return;
                }
                if (!decimal.TryParse(txtSHSL.Text, out fshqty))
                {
                    Toast.MakeText(this, "收货数量格式不正确！", 0).Show();
                    return;
                }
                txtSHSL.Text = fshqty.ToString(GlobalDataCache.QtyFormat);
                fshqty = Convert.ToDecimal(fshqty);
                if (fshqty < 0)
                {
                    Toast.MakeText(this, "收货数量必须大于或等于0！", 0).Show();
                    return;
                }
                if (fshqty >= 1000000)
                {
                    Toast.MakeText(this, "收货数量必须小于一百万！", 0).Show();
                    return;
                }

                //this.CurrentEditGoodPHInfo.fshqty = fshqty;
                //lsvPHDetail.Adapter = new PDLRGoodsInfoAdapter(this, CurrentEditGoodPHInfoList);
                //this.SaveLRData(this.CurrentEditGoodPHInfo);
            }
        }

        private void txtPH_KeyPress(object sender, View.KeyEventArgs e)
        {
            e.Handled = false;
            if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Up)
            {
                //var currPH =CurrentEditGoods.PHList.Where()
                if (CurrentEditGoodPHInfoList == null)
                {
                    return;
                }
                var currPH = CurrentEditGoodPHInfoList.Where(p => p.cph == txtPH.Text.Trim()).FirstOrDefault();
                if (currPH != null)
                {
                    this.txtSCDATE.Text = currPH.dmadedate;
                    this.txtYXDATE.Text = currPH.dexpdate;
                    this.txtSXDATE.Text = currPH.cphnote3;
                    GetFILETEXT();
                    this.txtSHSL.RequestFocus();
                    this.txtSHSL.FindFocus();
                }
                else
                {
                    this.txtSCDATE.Focusable = true;
                    this.txtSCDATE.FocusableInTouchMode = true;
                    this.txtSCDATE.RequestFocus();
                    this.txtSCDATE.FindFocus();
                }
                e.Handled = true;
            }
        }

        private void Scdate_KeyPress(object sender, View.KeyEventArgs e)
        {
            e.Handled = false;
            if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Up)
            {
                //var dmadedate = this.txtSCDATE.Text.ToString().Trim();
                var dmadedate = ConvertHelper.ToDatetTimeString(this.txtSCDATE.Text.ToString().Trim());
                if (!string.IsNullOrWhiteSpace(dmadedate))
                {
                    if (!ConvertHelper.IsLegalDateTime(dmadedate))
                    {
                        Toast.MakeText(this, "产期不正确！", 0).Show();
                        return;
                    }
                    else if (Convert.ToDateTime(dmadedate) > System.DateTime.Now)
                    {
                        Toast.MakeText(this, "产期[" + Convert.ToDateTime(dmadedate).ToString("yyyy-MM-dd") + "]不允许大于当前日期[" + System.DateTime.Now.ToString("yyyy-MM-dd") + "]", 0).Show();
                        this.txtSCDATE.Text = "";
                        return;
                    }
                    else
                    {
                        this.txtSCDATE.Text = dmadedate;
                    }
                    if (this.CurrentEditGoods != null && this.CurrentEditGoods.iterm > 0)
                    {
                        txtYXDATE.Text = Convert.ToDateTime(this.txtSCDATE.Text).AddMonths(this.CurrentEditGoods.iterm).AddDays(-1).ToString("yyyy-MM-dd");
                        if (Convert.ToDateTime(txtYXDATE.Text) < System.DateTime.Now)
                        {
                            Toast.MakeText(this, "效期[" + Convert.ToDateTime(txtYXDATE.Text).ToString("yyyy-MM-dd") + "]不允许小于于当前日期[" + System.DateTime.Now.ToString("yyyy-MM-dd") + "]", 0).Show();
                            txtYXDATE.Text = "";
                            return;
                        }
                    }
                    this.txtYXDATE.Focusable = true;
                    this.txtYXDATE.FocusableInTouchMode = true;
                    this.txtYXDATE.RequestFocus();
                    this.txtYXDATE.FindFocus();
                    e.Handled = true;
                }

            }
        }

        private void YXdate_KeyPress(object sender, View.KeyEventArgs e)
        {
            e.Handled = false;
            if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Up)
            {
                //var dexpdate = this.txtYXDATE.Text.ToString().Trim();
                var dexpdate = ConvertHelper.ToDatetTimeString(this.txtYXDATE.Text.ToString().Trim());
                if (!string.IsNullOrWhiteSpace(dexpdate))
                {
                    if (!ConvertHelper.IsLegalDateTime(dexpdate))
                    {
                        Toast.MakeText(this, "有效期不正确！", 0).Show();
                        return;
                    }
                    if (Convert.ToDateTime(dexpdate) < System.DateTime.Now)
                    {
                        Toast.MakeText(this, "效期[" + Convert.ToDateTime(dexpdate).ToString("yyyy-MM-dd") + "]不允许小于于当前日期[" + System.DateTime.Now.ToString("yyyy-MM-dd") + "]", 0).Show();
                        this.txtYXDATE.Text = "";
                        return;
                    }
                    else
                    {
                        this.txtYXDATE.Text = dexpdate;
                    }
                    if (string.IsNullOrEmpty(this.txtSCDATE.Text) && this.CurrentEditGoods != null && this.CurrentEditGoods.iterm > 0)
                    {
                        this.txtSCDATE.Text = Convert.ToDateTime(this.txtYXDATE.Text).AddMonths(-this.CurrentEditGoods.iterm).AddDays(1).ToString("yyyy-MM-dd");
                        if (Convert.ToDateTime(this.txtSCDATE.Text) > System.DateTime.Now)
                        {
                            Toast.MakeText(this, "产期[" + Convert.ToDateTime(this.txtSCDATE.Text).ToString("yyyy-MM-dd") + "]不允许大于当前日期[" + System.DateTime.Now.ToString("yyyy-MM-dd") + "]", 0).Show();
                            this.txtSCDATE.Text = "";
                            return;
                        }
                    }
                    GetFILETEXT();
                }
                this.txtSHSL.Focusable = true;
                this.txtSHSL.FocusableInTouchMode = true;
                this.txtSHSL.RequestFocus();
                this.txtSHSL.FindFocus();
                e.Handled = true;
            }
        }

        private void SXdate_KeyPress(object sender, View.KeyEventArgs e)
        {
            e.Handled = false;
            if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Up)
            {
                //var dmadedate = this.txtSCDATE.Text.ToString().Trim();
                var dsxdate = ConvertHelper.ToDatetTimeString(this.txtSXDATE.Text.ToString().Trim());
                if (!string.IsNullOrWhiteSpace(dsxdate))
                {
                    if (!ConvertHelper.IsLegalDateTime(dsxdate))
                    {
                        Toast.MakeText(this, "产期不正确！", 0).Show();
                        return;
                    }
                    else if (Convert.ToDateTime(dsxdate) > System.DateTime.Now)
                    {
                        Toast.MakeText(this, "产期[" + Convert.ToDateTime(dsxdate).ToString("yyyy-MM-dd") + "]不允许大于当前日期[" + System.DateTime.Now.ToString("yyyy-MM-dd") + "]", 0).Show();
                        this.txtSCDATE.Text = "";
                        return;
                    }
                    else
                    {
                        this.txtSCDATE.Text = dsxdate;
                    }
                    e.Handled = true;
                }

            }
        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            if (this.btnNewPH.Text == "完成")
            {
                Toast.MakeText(this, "新建批号未完成，不允许此操作！", 0).Show();
                return;
            }
            for (int i = 0; i < e.Parent.ChildCount; i++)
            {
                var cv = e.Parent.GetChildAt(i);
                //cv.SetBackgroundResource(0);

                var other_list_txt_ph = cv.FindViewById<TextView>(Resource.Id.txt_ph);
                var other_list_txt_pdsl = cv.FindViewById<TextView>(Resource.Id.txt_pdsl);
                var other_list_txt_kcsl = cv.FindViewById<TextView>(Resource.Id.txt_kcsl);
                var other_list_txt_cysl = cv.FindViewById<TextView>(Resource.Id.txt_cysl);

                other_list_txt_ph.SetTextColor(custom_blue);
                //other_list_txt_pdsl.SetTextColor(custom_blue);
                //other_list_txt_kcsl.SetTextColor(custom_blue);
                //other_list_txt_cysl.SetTextColor(custom_blue);
            }
            //(e.View).SetBackgroundResource(Resource.Drawable.rectangle);
            //(e.View).SetBackgroundColor(custom_blue);

            #region 设置字体颜色

            //var list_txt_ph = e.View.FindViewById<TextView>(Resource.Id.txt_ph);
            //var list_txt_pdsl = e.View.FindViewById<TextView>(Resource.Id.txt_pdsl);
            //var list_txt_kcsl = e.View.FindViewById<TextView>(Resource.Id.txt_kcsl);
            //var list_txt_cysl = e.View.FindViewById<TextView>(Resource.Id.txt_cysl);

            //list_txt_ph.SetTextColor(custom_blue);
            //list_txt_pdsl.SetTextColor(custom_blue);
            //list_txt_kcsl.SetTextColor(custom_blue);
            //list_txt_cysl.SetTextColor(custom_blue);
            #endregion

            CurrentEditGoodPHInfo = ((e.View).Tag as SHLRGoodsInfoViewHolder).Data;
            if (CurrentEditTrace12List != null && CurrentEditTrace12List.Count > 0)
            {
                CurrentTempEditTrace12 = CurrentEditTrace12List.Where(a => a.cgoodsid == CurrentEditGoodPHInfo.cgoodsid && a.cph == CurrentEditGoodPHInfo.cph).FirstOrDefault();
            }
            if (CurrentEditTrace13List != null && CurrentEditTrace13List.Count > 0)
            {
                CurrentTempEditTrace13 = CurrentEditTrace13List.Where(a => a.cgoodsid == CurrentEditGoodPHInfo.cgoodsid && a.cph == CurrentEditGoodPHInfo.cph).FirstOrDefault();
            }
            this.txtPH.Text = CurrentEditGoodPHInfo.cph;
            this.txtSHSL.Text = CurrentEditGoodPHInfo.fshqty.ToString(GlobalDataCache.QtyFormat);
            this.txtQSSL.Text = CurrentEditGoodPHInfo.fqsqty.ToString(GlobalDataCache.QtyFormat);
            this.txtSCDATE.Text = CurrentEditGoodPHInfo.dmadedate;
            this.txtYXDATE.Text = CurrentEditGoodPHInfo.dexpdate;
            this.txtSXDATE.Text = CurrentEditGoodPHInfo.cphnote3;
            GetFILETEXT();
            this.spSeason.SetSelection(rejectionReasons.FindIndex(a => a.Equals(CurrentEditGoodPHInfo.crejectseason)));
        }

        private void ListView_ChildViewAdded(object sender, ViewGroup.ChildViewAddedEventArgs e)
        {
            e.Child.SetBackgroundResource(0);
            if (this.CurrentEditGoods == null)
            {
                return;
            }
            if ((e.Child.Tag as SHLRGoodsInfoViewHolder).Data == this.CurrentEditGoodPHInfo)
            {
                //(e.Child as TextView).SetTextColor(custom_blue);
                //e.Child.SetBackgroundResource(Resource.Drawable.rectangle);

                var list_txt_ph = e.Child.FindViewById<TextView>(Resource.Id.txt_ph);
                var list_txt_pdsl = e.Child.FindViewById<TextView>(Resource.Id.txt_pdsl);
                var list_txt_kcsl = e.Child.FindViewById<TextView>(Resource.Id.txt_kcsl);
                var list_txt_cysl = e.Child.FindViewById<TextView>(Resource.Id.txt_cysl);

                //list_txt_ph.SetTextColor(custom_blue);
                //list_txt_pdsl.SetTextColor(custom_blue);
                //list_txt_kcsl.SetTextColor(custom_blue);
                //list_txt_cysl.SetTextColor(custom_blue);
            }
        }

        private void spSeasonInit()
        {
            this.Proxy.Execute(new PDASHOPRequest()
            {
                OPType = 5,
                EmpCode = GlobalProxySetting.GetLoginState().EmployeeCode,
                cbilid = CurrentSHMainInfo.cbilid
            }, (response) =>
            {
                if (!response.IsError)
                {

                    //var rejectionReasons = response.RejectionReasonsList.Select(c => c.ccodetext).ToArray();
                    rejectionReasons = new List<string>();
                    rejectionReasons = response.RejectionReasonsList.Select(c => c.ccodetext).ToList();
                    rejectionReasons.Add("");
                    //spSeason.Adapter = new K9PDA.Adapter.SpinnerAdapter(this, Android.Resource.Layout.SimpleSpinnerItem, new List<string>() { "采购订单", "销售退回申请", "门店退回申请" });
                    spSeason.Adapter = new K9PDA.Adapter.SpinnerAdapter(this, Android.Resource.Layout.SimpleSpinnerItem, rejectionReasons);
                    spSeason.SetSelection(rejectionReasons.Count() - 1);
                }
            }, this);

        }

        private void BtnNewPH_Click(object sender, EventArgs e)
        {

            this.btnNewPH.FocusableInTouchMode = false;
            if (this.CurrentEditGoods == null)
            {
                Toast.MakeText(this, "请扫描商品！", 0).Show();
                return;
            }

            var ysshlist = CurrentEditGoodsList.Where(a => a.cgoodsid == CurrentEditGoods.cgoodsid).ToList();

            decimal reffqty = CheckYshqty();

            if (IsUseNewScanFlow)
            {
                var bresult = false;
                if (bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
                {
                    if (CurrentEditTrace12List == null) CurrentEditTrace12List = new List<GoodsTrace12>();
                    if (CurrentEditTrace13List == null) CurrentEditTrace13List = new List<GoodsTrace13>();
                    var dtmpfqty2 = ChooseGoodsInfo[0].fqty * scanCount;
                    if (reffqty < dtmpfqty2)
                    {
                        Toast.MakeText(this, "组合条码扫描数量[" + dtmpfqty2.ToString() + "]不能大于收货数量[" + reffqty.ToString() + "]，不允许此操作！", 0).Show();
                        txtSHSL.Text = "";
                        txtINPUT.Text = "";
                        return;
                    }
                }
            }

            bool UDIZHTMqty = false;
            //#region 计算组合条码的扫描数量
            //if (bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)    //UDI计算数量
            //{
            //    var bresutl = false;
            //    if ((CurrentEditTrace12List != null && CurrentEditTrace12List.Count > 0 && CurrentEditTrace12List.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph == ChooseGoodsInfo[0].cph).Any()) || (CurrentEditTrace13List != null && CurrentEditTrace13List.Count > 0 && CurrentEditTrace13List.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph == ChooseGoodsInfo[0].cph).Any()))
            //    {
            //        reffqty = CoumTracefqty(CurrentEditGoods, "", ref bresutl);
            //        UDIZHTMqty = true;
            //    }
            //}
            //#endregion

            if (this.btnNewPH.Text == "新建批号")
            {
                if (reffqty <= 0)
                {
                    Toast.MakeText(this, "拒收数量+收货数量要小于或等于通知数量！", 0).Show();
                    return;
                }
                if (CurrentEditGoods.ishtype != 1 && CurrentEditGoods.PHList.Count() != 0)
                {
                    Toast.MakeText(this, "销售退回申请或门店退回申请不能新建批号！", 0).Show();
                    return;
                }
                if (bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)    //UDI计算数量
                {
                    //判断如果前面已经扫描过这个品种品号，则需要在原来的基础上加1
                    if (CurrentEditGoodsList != null && CurrentEditGoodsList.Count > 0 && iSamePHProcess > 1 && CurrentEditGoodsList.Where(p => p.cgoodsid == ChooseGoodsInfo[0].cgoodsid && p.cph == ChooseGoodsInfo[0].cph && p.dmadedate == ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate) && p.dexpdate == ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate) && p.ref_cbilid == CurrentEditGoods.ref_cbilid && p.id1 == CurrentEditGoods.id1).Any())
                    {
                        var tmpcur = CurrentEditGoodsList.Where(p => p.cgoodsid == ChooseGoodsInfo[0].cgoodsid && p.cph == ChooseGoodsInfo[0].cph && p.ref_cbilid == CurrentEditGoods.ref_cbilid && p.dmadedate == ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate) && p.dexpdate == ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate) && p.id1 == CurrentEditGoods.id1).FirstOrDefault();
                        if (tmpcur != null && tmpcur.PHList.Count > 0)
                        {
                            this.txtSHSL.Text = (tmpcur.PHList.Sum(s => s.fshqty) + ChooseGoodsInfo[0].fqty).ToString(GlobalDataCache.QtyFormat);
                        }
                        else
                        {
                            this.txtSHSL.Text = ChooseGoodsInfo[0].fqty.ToString(GlobalDataCache.QtyFormat);
                        }
                    }
                    else if (CurrentEditGoodsList != null && CurrentEditGoodsList.Count > 0 && iSamePHProcess <= 1 && CurrentEditGoodsList.Where(p => p.cgoodsid == ChooseGoodsInfo[0].cgoodsid && p.cph == ChooseGoodsInfo[0].cph && p.ref_cbilid == CurrentEditGoods.ref_cbilid && p.id1 == CurrentEditGoods.id1).Any())
                    {
                        var tmpcur = CurrentEditGoodsList.Where(p => p.cgoodsid == ChooseGoodsInfo[0].cgoodsid && p.cph == ChooseGoodsInfo[0].cph && p.ref_cbilid == CurrentEditGoods.ref_cbilid && p.id1 == CurrentEditGoods.id1).FirstOrDefault();
                        if (tmpcur != null && tmpcur.PHList.Count > 0)
                        {
                            this.txtSHSL.Text = (tmpcur.PHList.Sum(s => s.fshqty) + ChooseGoodsInfo[0].fqty).ToString(GlobalDataCache.QtyFormat);
                        }
                        else
                        {
                            this.txtSHSL.Text = ChooseGoodsInfo[0].fqty.ToString(GlobalDataCache.QtyFormat);
                        }
                    }
                    else
                    {
                        this.txtSHSL.Text = ChooseGoodsInfo[0].fqty.ToString(GlobalDataCache.QtyFormat);
                    }
                }
                else
                {
                    this.txtSHSL.Text = reffqty.ToString(GlobalDataCache.QtyFormat);
                }
                this.txtQSSL.Text = "0";
                this.btnNewPH.Text = "完成";
                this.txtINPUT.Enabled = false;
                if (CurrentEditGoods.ishtype == 1)
                {
                    this.txtSCDATE.Enabled = true;
                    this.txtYXDATE.Enabled = true;
                    this.txtSXDATE.Enabled = true;
                    this.txtPH.Enabled = true;
                    this.ClearEditControl();

                    if (!string.IsNullOrWhiteSpace(infoQr.cgoodsid))//采购入库，二维码扫码直接赋值
                    {
                        if (!string.IsNullOrWhiteSpace(infoQr.cph))//批号
                        {
                            this.txtPH.Text = infoQr.cph;
                        }
                        if (!string.IsNullOrWhiteSpace(infoQr.dmadedate))
                        {
                            this.txtSCDATE.Text = infoQr.dmadedate;
                        }
                        if (!string.IsNullOrWhiteSpace(infoQr.dexpdate))
                        {
                            this.txtYXDATE.Text = infoQr.dexpdate;
                        }
                        if (!string.IsNullOrWhiteSpace(infoQr.cphnote3))
                        {
                            this.txtSXDATE.Text = infoQr.cphnote3;
                        }
                    }
                    #region 组合条码
                    if (bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)//组合条码解析
                    {
                        if (!string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].cph))//批号
                        {
                            this.txtPH.Text = ChooseGoodsInfo[0].cph;
                            if (CurrentEditGoods != null) CurrentEditGoods.cph = ChooseGoodsInfo[0].cph;
                        }
                        if (!string.IsNullOrWhiteSpace(ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate)))
                        {
                            this.txtSCDATE.Text = ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate);
                            if (CurrentEditGoods != null) CurrentEditGoods.dmadedate = ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate);
                        }
                        if (!string.IsNullOrWhiteSpace(ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate)))
                        {
                            this.txtYXDATE.Text = ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate);
                            if (CurrentEditGoods != null) CurrentEditGoods.dexpdate = ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate);
                        }
                        if (!string.IsNullOrWhiteSpace(ConvertHelper.ToString(ChooseGoodsInfo[0].cphnote3)))
                        {
                            this.txtYXDATE.Text = ConvertHelper.ToString(ChooseGoodsInfo[0].cphnote3);
                            if (CurrentEditGoods != null) CurrentEditGoods.dexpdate = ConvertHelper.ToString(ChooseGoodsInfo[0].cphnote3);
                        }
                        GetFILETEXT();
                    }
                    #endregion

                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(infoQr.cgoodsid) && (infoQr.cph != CurrentEditGoods.cph))
                    {
                        Toast.MakeText(this, "系统批号与扫码批号不相等，请检查！", 0).Show();
                        return;
                    }

                    if (bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)//组合条码解析
                    {
                        if (!string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].cgoodsid) && !string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].cph) && (!CurrentEditGoods.cph.Contains(ChooseGoodsInfo[0].cph)))
                        {
                            Toast.MakeText(this, "系统批号与扫码批号不相等，请检查！", 0).Show();
                            return;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(infoQr.cgoodsid) && (infoQr.cph == CurrentEditGoods.cph))
                    {
                        if (!string.IsNullOrWhiteSpace(infoQr.cph))//批号
                        {
                            this.txtPH.Text = infoQr.cph;
                        }
                        if (!string.IsNullOrWhiteSpace(infoQr.dmadedate))
                        {
                            this.txtSCDATE.Text = infoQr.dmadedate;
                        }
                        if (!string.IsNullOrWhiteSpace(infoQr.dexpdate))
                        {
                            this.txtYXDATE.Text = infoQr.dexpdate;
                        }
                        if (!string.IsNullOrWhiteSpace(infoQr.cphnote3))
                        {
                            this.txtYXDATE.Text = infoQr.cphnote3;
                        }
                    }
                    #region 组合条码
                    else if (bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && !string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].cgoodsid) && !string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].cph) && (CurrentEditGoods.cph.Contains(ChooseGoodsInfo[0].cph)))//组合条码解析
                    {
                        if (GlobalDataCache.p_SOFTUSERTYPE == "zz579" && CurrentSHMainInfo != null && CurrentSHMainInfo.ishtype != 1 && CurrentEditGoods != null) //按原单批号产期，有效期
                        {
                            this.txtPH.Text = CurrentEditGoods.cph;
                            this.txtSCDATE.Text = CurrentEditGoods.dmadedate;
                            this.txtYXDATE.Text = CurrentEditGoods.dexpdate;
                            this.txtSXDATE.Text = CurrentEditGoods.cphnote3;
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].cph))//批号
                            {
                                this.txtPH.Text = ChooseGoodsInfo[0].cph;
                                if (CurrentEditGoods != null) CurrentEditGoods.cph = ChooseGoodsInfo[0].cph;
                            }
                            if (!string.IsNullOrWhiteSpace(ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate)))
                            {
                                this.txtSCDATE.Text = ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate);
                                if (CurrentEditGoods != null) CurrentEditGoods.dmadedate = ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate);
                            }
                            else if (string.IsNullOrWhiteSpace(ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate)) && !string.IsNullOrEmpty(CurrentEditGoods.dmadedate))//当UDI解析不存在产期时。取源单产期
                            {
                                this.txtSCDATE.Text = CurrentEditGoods.dmadedate;
                            }
                            if (!string.IsNullOrWhiteSpace(ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate)))
                            {
                                this.txtYXDATE.Text = ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate);
                                if (CurrentEditGoods != null) CurrentEditGoods.dexpdate = ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate);
                            }
                            else if (string.IsNullOrWhiteSpace(ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate)) && !string.IsNullOrEmpty(CurrentEditGoods.dexpdate))//当UDI解析不存在有效期时。取源单有效期
                            {
                                this.txtYXDATE.Text = CurrentEditGoods.dexpdate;
                            }
                            if (!string.IsNullOrWhiteSpace(ConvertHelper.ToString(ChooseGoodsInfo[0].cphnote3)))
                            {
                                this.txtSXDATE.Text = ConvertHelper.ToString(ChooseGoodsInfo[0].cphnote3);
                                if (CurrentEditGoods != null) CurrentEditGoods.cphnote3 = ConvertHelper.ToString(ChooseGoodsInfo[0].cphnote3);
                            }
                            else if (string.IsNullOrWhiteSpace(ConvertHelper.ToString(ChooseGoodsInfo[0].cphnote3)) && !string.IsNullOrEmpty(CurrentEditGoods.cphnote3))//当UDI解析不存在有效期时。取源单有效期
                            {
                                this.txtSXDATE.Text = CurrentEditGoods.cphnote3;
                            }
                        }
                        GetFILETEXT();
                    }
                    #endregion
                    else
                    {
                        this.txtPH.Text = CurrentEditGoods.cph;
                        this.txtSCDATE.Text = CurrentEditGoods.dmadedate;
                        this.txtYXDATE.Text = CurrentEditGoods.dexpdate;
                        this.txtSXDATE.Text = CurrentEditGoods.cphnote3;
                        GetFILETEXT();
                    }
                    this.txtSCDATE.Enabled = false;
                    this.txtYXDATE.Enabled = false;
                    this.txtSXDATE.Enabled = false;
                    this.txtPH.Enabled = false;
                }
                this.txtSHSL.Enabled = true;
                this.txtQSSL.Enabled = true;
                this.spSeason.Enabled = true;
                this.btnDelPH.Visibility = ViewStates.Invisible;
                this.txtPH.Focusable = true;
                this.txtPH.FocusableInTouchMode = true;
                if (bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)//扫描成功后，直接光标再定位回扫品种
                {
                    //AddCurrentEditTraceList(CurrentEditGoods);
                    SaveLRData(CurrentEditGoods);
                    //if (GlobalDataCache.p_SOFTUSERTYPE == "zz579") BtnNewPH_Click(null, null);

                    this.txtINPUT.Focusable = true;
                    this.txtINPUT.FocusableInTouchMode = true;
                    this.txtINPUT.Enabled = true;
                    this.txtINPUT.RequestFocus();

                }
                else
                {
                    this.txtPH.RequestFocus();
                }
            }
            else
            {
                var cph = txtPH.Text.Trim();
                if (iSamePHProcess > 1) cph = txtPH.Text;//2024-06-24 
                if (string.IsNullOrWhiteSpace(cph) && this.CurrentEditGoods.iphflag == 1)
                {
                    Toast.MakeText(this, "批号不能为空！", 0).Show();
                    return;
                }
                //if (cph !="")
                //{                    
                //    if (!this.CurrentEditGoodPHInfoList.Any(p => p.cph == cph) && CurrentSHMainInfo.ishtype !=1)
                //    {
                //        Toast.MakeText(this, "批号不存在！", 0).Show();
                //        return;
                //    }
                //    //if (!this.CurrentEditGoods.PHList.Any(p => p.cph == cph))
                //    //{
                //    //    Toast.MakeText(this, "批号已存在当前记录中！", 0).Show();
                //    //    return;
                //    //}
                //}
                var dmadedate = txtSCDATE.Text.ToString().Trim();
                var dexpdate = txtYXDATE.Text.ToString().Trim();
                var cphnote3 = txtSXDATE.Text.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(dmadedate) && !ConvertHelper.IsLegalDateTime(dmadedate))
                {
                    Toast.MakeText(this, "产期不正确！", 0).Show();
                    return;
                }
                if (!string.IsNullOrWhiteSpace(dexpdate) && !ConvertHelper.IsLegalDateTime(dexpdate))
                {
                    Toast.MakeText(this, "有效期不正确！", 0).Show();
                    return;
                }
                if (!string.IsNullOrWhiteSpace(dexpdate))
                {
                    if (Convert.ToDateTime(dexpdate) <= DateTime.Now)
                    {
                        Toast.MakeText(this, "有效期[" + dexpdate + "]不允许小于当前服务器日期[" + DateTime.Now.ToString("yyyyMMdd") + "]", 0).Show();
                        return;
                    }
                }

                if (!string.IsNullOrWhiteSpace(cphnote3) && !ConvertHelper.IsLegalDateTime(cphnote3))
                {
                    Toast.MakeText(this, "有效期不正确！", 0).Show();
                    return;
                }
                if (!string.IsNullOrWhiteSpace(cphnote3))
                {
                    if (Convert.ToDateTime(cphnote3) <= DateTime.Now)
                    {
                        Toast.MakeText(this, "有效期[" + cphnote3 + "]不允许小于当前服务器日期[" + DateTime.Now.ToString("yyyyMMdd") + "]", 0).Show();
                        return;
                    }
                }

                decimal fshqty = 0;
                if (string.IsNullOrWhiteSpace(txtSHSL.Text))
                {
                    Toast.MakeText(this, "收货数量不能为空！", 0).Show();
                    return;
                }
                if (!decimal.TryParse(txtSHSL.Text, out fshqty))
                {
                    Toast.MakeText(this, "收货数量格式不正确！", 0).Show();
                    return;
                }
                this.txtSHSL.Text = fshqty.ToString(GlobalDataCache.QtyFormat);
                fshqty = Convert.ToDecimal(fshqty.ToString(GlobalDataCache.QtyFormat));
                if (fshqty < 0)
                {
                    Toast.MakeText(this, "收货数量必须大于或等于0！", 0).Show();
                    return;
                }
                decimal fqsqty = 0;
                if (!decimal.TryParse(txtQSSL.Text, out fqsqty))
                {
                    Toast.MakeText(this, "拒收数量格式不正确！", 0).Show();
                    return;
                }
                if (fqsqty != 0 && spSeason.SelectedItemId == rejectionReasons.Count() - 1)
                {
                    Toast.MakeText(this, "商品" + CurrentEditGoods.cgoodsid + "，拒收原因不能为空！", 0).Show();
                    return;
                }
                this.txtQSSL.Text = fqsqty.ToString(GlobalDataCache.QtyFormat);
                fqsqty = Convert.ToDecimal(fqsqty.ToString(GlobalDataCache.QtyFormat));
                if (fqsqty < 0)
                {
                    Toast.MakeText(this, "收货数量必须大于或等于0！", 0).Show();
                    return;
                }
                if (fshqty + fqsqty > reffqty)
                {
                    Toast.MakeText(this, "拒收数量+收货数量要小于或等于通知数量！", 0).Show();
                    return;
                }
                reffqty = CurrentEditGoods.ref_fqty - CurrentEditGoods.PHList.Sum(p => p.fshqty + p.fqsqty);
                #region UDI扫码校验收货数量不能大于通知数量
                //CurrentEditGoodsList
                if (bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)    //UDI计算数量
                {
                    if (CurrentEditGoodsList != null && CurrentEditGoodsList.Count > 0 && CurrentEditGoodsList.Any(p => p.cgoodsid == CurrentEditGoods.cgoodsid))
                    {
                        decimal fcshqty = 0;
                        if (CurrentEditGoods.ishtype == 1)//采购订单
                        {
                            var tmplist = CurrentEditGoodsList.Where(a => a.ref_cbilid == CurrentEditGoods.ref_cbilid && a.cgoodsid == CurrentEditGoods.cgoodsid && a.cph != CurrentEditGoods.cph).ToList();

                            if (tmplist != null && tmplist.Count > 0)
                            {
                                for (int i = 0; i < tmplist.Count; i++)
                                {
                                    fcshqty += tmplist[i].PHList.Sum(p => p.fshqty + p.fqsqty);
                                }
                            }
                        }
                        if (CurrentEditGoods.ref_fqty < fcshqty + fshqty + fqsqty)
                        {
                            Toast.MakeText(this, "拒收数量+收货数量要小于或等于通知数量！", 0).Show();
                            return;
                        }
                    }
                }
                #endregion
                /*新增批号*/
                if (this.CurrentEditGoods.PHList.Any(p => p.cph == cph))
                {
                    decimal fcshqty = 0;
                    if (CurrentEditGoods.ishtype == 1)//采购订单
                    {
                        var tmplist = CurrentEditGoodsList.Where(a => a.ref_cbilid == CurrentEditGoods.ref_cbilid && a.cgoodsid == CurrentEditGoods.cgoodsid && a.cph != cph).ToList();

                        if (tmplist != null && tmplist.Count > 0)
                        {
                            for (int i = 0; i < tmplist.Count; i++)
                            {
                                fcshqty += tmplist[i].PHList.Sum(p => p.fshqty + p.fqsqty);
                            }
                        }
                    }
                    if (CurrentEditGoods.ref_fqty < fcshqty + fshqty + fqsqty)
                    {
                        Toast.MakeText(this, "拒收数量+收货数量要小于或等于通知数量！", 0).Show();
                        return;
                    }
                    var currGoods = CurrentEditGoods.PHList.Where(p => p.cph == cph).FirstOrDefault();
                    currGoods.dexpdate = dexpdate;
                    currGoods.dmadedate = dmadedate;
                    currGoods.cphnote3 = cphnote3;
                    if (IsEnableSFDARenewal)
                    {
                        GetFILETEXT();
                        currGoods.cphnote2 = this.txtFILE.Text;
                    }
                    //currGoods.fqty = reffqty;
                    currGoods.fshqty = fshqty;
                    currGoods.fqsqty = fqsqty;
                    CurrentEditGoodPHInfo = currGoods;
                    currGoods.crejectseason = rejectionReasons[(int)spSeason.SelectedItemId];
                }
                else
                {
                    Modifyfqty();
                    SHPHInfo newgoods = new SHPHInfo();
                    newgoods.cgoodsid = CurrentEditGoods.cgoodsid;
                    newgoods.cgoodsname = CurrentEditGoods.cgoodsname;
                    newgoods.cph = cph;
                    newgoods.dexpdate = dexpdate;
                    newgoods.dmadedate = dmadedate;
                    newgoods.fqty = reffqty;
                    newgoods.fshqty = fshqty;
                    newgoods.fqsqty = fqsqty;
                    if (IsEnableSFDARenewal)
                    {
                        GetFILETEXT();
                        newgoods.cphnote2 = this.txtFILE.Text;
                    }
                    newgoods.crejectseason = rejectionReasons[(int)spSeason.SelectedItemId];
                    CurrentEditGoodPHInfo = newgoods;

                    CurrentEditGoods.PHList.Add(newgoods);
                }
                /*更新本地操作的商品明细记录数据*/
                SaveLRData(CurrentEditGoods, this.btnNewPH.Text);
                listView.Adapter = new SHLRGoodsInfoAdapter(this, CurrentEditGoods.PHList);

                this.txtINPUT.Enabled = true;
                this.txtSCDATE.Enabled = false;
                this.txtYXDATE.Enabled = false;
                this.txtPH.Enabled = false;
                this.txtSHSL.Enabled = false;
                this.txtQSSL.Enabled = false;
                this.spSeason.Enabled = false;
                this.btnNewPH.Text = "新建批号";
                this.btnDelPH.Visibility = ViewStates.Visible;
                this.ClearEditControl();
                Decimal shqty = 0;
                this.txtSHSL.Text = shqty.ToString(GlobalDataCache.QtyFormat);//完成清空收货数量
                this.txtQSSL.Text = shqty.ToString(GlobalDataCache.QtyFormat);
                if (bchkUDI)//KB023 2023-04-26 完成后，清空当前编辑的药品明细 
                {
                    CurrentEditGoods = null;
                    _lastCode = "";
                    UDIlastCode = "";
                    _lastUDIRuleLen = 0;
                    UDIastUDIRuleLen = 0;
                    _Rulecode = "";
                    UDIRulecode = "";
                    _LastUDIgoodsid = "";
                }
                this.txtINPUT.RequestFocus();
            }
        }

        private void Modifyfqty()
        {
            foreach (var item in CurrentEditGoods.PHList)
            {
                if (item.fshqty + item.fqsqty != item.fqty)
                {
                    item.fqty = item.fshqty + item.fqsqty;
                }
            }
        }
        private void GetFILETEXT()
        {
            try
            {
                if (CurrentEditGoods == null) return;
                if (string.IsNullOrEmpty(this.txtSCDATE.Text)) return;
                if (cfilenofilelist == null) return;
                if (cfilenofilelist.Where(a => a.cgoodsid == CurrentEditGoods.cgoodsid).Any())
                {
                    var filelist = cfilenofilelist.Where(a => a.cgoodsid == CurrentEditGoods.cgoodsid && ConvertHelper.ToDateTime(a.dfilenodate) >= ConvertHelper.ToDateTime(this.txtSCDATE.Text)).OrderBy(d => ConvertHelper.ToDateTime(d.dfilenodate)).ToList();
                    if (filelist != null && filelist.Count > 0)
                    {
                        txtFILE.Text = filelist.FirstOrDefault().cfileno;
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }

        }
        private void BtnDelPH_Click(object sender, EventArgs e)
        {
            if (this.CurrentEditGoodPHInfo == null)
            {
                Toast.MakeText(this, "请选择要删除的批号！", 0).Show();
                return;
            }
            CustomAlertDialog customAlertDialog = new CustomAlertDialog();
            customAlertDialog.OKClick += CustomAlertDialog_OKClick;
            //txtdetail.Text = "是否删除批号【" + this.CurrentEditGoodPHInfo.cph + "】？";
            customAlertDialog.AlertDialogShow(this, "是否删除批号【" + this.CurrentEditGoodPHInfo.cph + "】？");
        }

        private void CustomAlertDialog_OKClick(object sender, EventArgs e)
        {
            if (CurrentEditGoods != null && this.CurrentEditGoods.PHList != null && this.CurrentEditGoods.PHList.Any(p => p.cph == CurrentEditGoodPHInfo.cph))
            {
                CurrentEditGoods.PHList.Remove(CurrentEditGoodPHInfo);
            }
            if (CurrentEditTrace12List != null && CurrentEditTrace12List.Count > 0 && CurrentTempEditTrace12 != null && !string.IsNullOrWhiteSpace(CurrentTempEditTrace12.cgoodsid))
            {
                for (int i = 0; i < CurrentEditTrace12List.Count; i++)
                {
                    if (CurrentEditTrace12List[i].id1 == CurrentTempEditTrace12.id1)
                    {
                        CurrentEditTrace12List.RemoveAt(i);
                    }
                }
                CurrentTempEditTrace12 = null;
                ChooseGoodsInfo = new List<InstrumentGoodsInfo>();
            }
            if (CurrentEditTrace13List != null && CurrentEditTrace13List.Count > 0 && CurrentTempEditTrace13 != null && !string.IsNullOrWhiteSpace(CurrentTempEditTrace13.cgoodsid))
            {
                for (int i = 0; i < CurrentEditTrace13List.Count; i++)
                {
                    if (CurrentEditTrace13List[i].id1 == CurrentTempEditTrace13.id1)
                    {
                        CurrentEditTrace13List.RemoveAt(i);
                    }
                }
                CurrentTempEditTrace13 = null;
                ChooseGoodsInfo = new List<InstrumentGoodsInfo>();
            }
            /*更新本地操作的商品明细记录数据*/
            if (CurrentEditGoods != null && CurrentEditGoods.PHList != null)
            {
                SaveLRData(CurrentEditGoods);
                listView.Adapter = new SHLRGoodsInfoAdapter(this, CurrentEditGoods.PHList);
            }
            else
                listView.Adapter = new SHLRGoodsInfoAdapter(this, new List<SHPHInfo>());
            this.CurrentEditGoodPHInfo = null;
            this.ClearEditControl();
            return;
        }

        private void CustomAlertDialog_SHOKClick(object sender, EventArgs e)
        {
            var bresult = false;
            txtPH.Text = CurrentEditGoods.cph;
            txtSCDATE.Text = CurrentEditGoods.dmadedate;
            txtYXDATE.Text = CurrentEditGoods.dexpdate;
            var tmp = CoumTracefqty(CurrentEditGoods, "check", ref bresult);
            this.txtSHSL.Text = tmp.ToString(GlobalDataCache.QtyFormat);
            this.txtQSSL.Text = CurrentEditGoods.frecrejectqty.ToString(GlobalDataCache.QtyFormat);
            BtnNewPH_Click(null, null);
        }

        private void CustomAlertDialog_SHCalcelClick(object sender, EventArgs e)
        {
            this.txtINPUT.Enabled = true;
            this.txtSCDATE.Enabled = false;
            this.txtYXDATE.Enabled = false;
            this.txtPH.Enabled = false;
            this.txtSHSL.Enabled = false;
            this.txtQSSL.Enabled = false;
            this.spSeason.Enabled = false;
            this.btnNewPH.Text = "新建批号";
            this.btnDelPH.Visibility = ViewStates.Visible;
            this.ClearEditControl();
            Decimal shqty = 0;
            this.txtSHSL.Text = shqty.ToString(GlobalDataCache.QtyFormat);//完成清空收货数量
            this.txtQSSL.Text = shqty.ToString(GlobalDataCache.QtyFormat);
            if (bchkUDI)//KB023 2023-04-26 完成后，清空当前编辑的药品明细 
            {
                CurrentEditGoods = null;
                _lastCode = "";
                UDIlastCode = "";
                _lastUDIRuleLen = 0;
                UDIastUDIRuleLen = 0;
                _Rulecode = "";
                UDIRulecode = "";
                _LastUDIgoodsid = "";
            }
            this.txtINPUT.RequestFocus();
        }

        private void spSeason_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var view = e.View as TextView;
            if (view != null)
            {
                //view.SetTextColor(Android.Content.Res.ColorStateList.ValueOf(Color.White));
                view.SetTextColor(custom_main_text);
            }
        }

        private void TB_NavigationOnClick(object sender, EventArgs e)
        {
            this.ReleaseBillEdit("DMSM01001210", CurrentSHMainInfo.cbilid);
            GlobalDataCache.ClearData("SHEditGoodsList");
            GlobalDataCache.ClearData("SHColdStoreInfo");
            GlobalDataCache.ClearData("temp_sh_cbilid");
            Finish();
        }

        private async Task txtINPUT_KeyPressAsync(object sender, View.KeyEventArgs e)
        {
            e.Handled = false;
            if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Up)
            {
                #region 组合条码
                if (bchkUDI)
                {
                    if (await DealUDICodeAsync())
                    {
                        if (IsUseNewScanFlow)
                        {
                            scanCount = udiScanSettingHelper.ScanCount;
                        }

                        if (!string.IsNullOrWhiteSpace(_lastCode))
                        {
                            txtINPUT.RequestFocus();
                            e.Handled = true;
                            return;
                        }
                        else if (CurrentEditGoods != null && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && CurrentEditTrace12List != null && CurrentEditTrace12List.Count > 0 && CurrentEditTrace12List.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph.Contains(ChooseGoodsInfo[0].cph)).Any() && CurrentEditGoods.cgoodsid == ChooseGoodsInfo[0].cgoodsid)
                        {
                            //修改当前编辑商品与扫描商品不一致时
                            if (this.btnNewPH.Text != "完成" && (CurrentEditGoods.cgoodsid != ChooseGoodsInfo[0].cgoodsid || !CurrentEditGoods.cph.Contains(ChooseGoodsInfo[0].cph)) && CurrentEditGoodsList != null && CurrentEditGoodsList.Count > 0 && CurrentEditGoodsList.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph.Contains(ChooseGoodsInfo[0].cph)).Any())
                            {
                                if (iSamePHProcess > 1)
                                {
                                    CurrentEditGoods = CurrentEditGoodsList.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph.Contains(ChooseGoodsInfo[0].cph) && a.dmadedate == ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate) && a.dexpdate == ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate)).FirstOrDefault();
                                    if (CurrentEditGoods != null && CurrentEditGoods.PHList != null)
                                        CurrentEditGoodPHInfo = CurrentEditGoods.PHList.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph.Contains(ChooseGoodsInfo[0].cph) && a.dmadedate == ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate) && a.dexpdate == ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate)).FirstOrDefault();
                                    else
                                        CurrentEditGoodPHInfo = null;
                                }
                                else
                                {
                                    CurrentEditGoods = CurrentEditGoodsList.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph == ChooseGoodsInfo[0].cph).FirstOrDefault();
                                    CurrentEditGoodPHInfo = CurrentEditGoods.PHList.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph == ChooseGoodsInfo[0].cph).FirstOrDefault();
                                }
                                if (CurrentEditGoodPHInfo != null)
                                {
                                    this.txtPH.Text = CurrentEditGoodPHInfo.cph;
                                    this.txtSHSL.Text = CurrentEditGoodPHInfo.fshqty.ToString(GlobalDataCache.QtyFormat);
                                    this.txtQSSL.Text = CurrentEditGoodPHInfo.fqsqty.ToString(GlobalDataCache.QtyFormat);
                                    this.txtSCDATE.Text = CurrentEditGoodPHInfo.dmadedate;
                                    this.txtYXDATE.Text = CurrentEditGoodPHInfo.dexpdate;
                                    GetFILETEXT();
                                    this.btnNewPH.Text = "完成";
                                }
                            }
                            if (CurrentEditGoods != null)
                            {
                                var tmpcph = CurrentEditTrace12List.Where(a => a.cgoodsid == CurrentEditGoods.cgoodsid && a.cbilid == (CurrentEditGoods.cbilid == "Add" ? CurrentEditGoods.ref_cbilid : CurrentEditGoods.cbilid) && a.cph == ChooseGoodsInfo[0].cph && ChooseGoodsInfo[0].cph == CurrentEditGoods.cph && a.id1 == CurrentEditGoods.id1).FirstOrDefault();
                                if (iSamePHProcess > 1)
                                {
                                    tmpcph = CurrentEditTrace12List.Where(a => a.cgoodsid == CurrentEditGoods.cgoodsid && a.cbilid == (CurrentEditGoods.cbilid == "Add" ? CurrentEditGoods.ref_cbilid : CurrentEditGoods.cbilid) && a.cph.Contains(ChooseGoodsInfo[0].cph) && CurrentEditGoods.cph.Contains(ChooseGoodsInfo[0].cph) && a.dmadedate == ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate) && a.dexpdate == ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate) && a.id1 == CurrentEditGoods.id1).FirstOrDefault();
                                }
                                if (GlobalDataCache.p_SOFTUSERTYPE == "zz579" && CurrentSHMainInfo.ishtype == 2)
                                {
                                    ////广州华亘朗博-器械
                                    tmpcph = CurrentEditTrace12List.Where(a => a.cgoodsid == CurrentEditGoods.cgoodsid && a.cparenttrace == ChooseGoodsInfo[0].cudicode && a.cph.Contains(ChooseGoodsInfo[0].cph) && a.dmadedate == CurrentEditGoods.dmadedate && a.dexpdate == CurrentEditGoods.dexpdate && a.id1 == CurrentEditGoods.id1).FirstOrDefault();
                                }
                                if (tmpcph != null)
                                {

                                    //更新数量 
                                    var bresult = false;
                                    decimal reffqty = CheckYshqty();
                                    if (reffqty - ChooseGoodsInfo[0].fqty <= 0)
                                    {
                                        Toast.MakeText(this, "拒收数量+收货数量要小于或等于通知数量！", 0).Show();
                                        txtINPUT.Text = "";
                                        e.Handled = true;
                                        return;
                                    }

                                    var tmp = CoumTracefqty(CurrentEditGoods, "check", ref bresult);

                                    if (!bresult)
                                    {
                                        this.txtSHSL.Text = tmp.ToString(GlobalDataCache.QtyFormat);
                                        SaveLRData(CurrentEditGoods);
                                    }
                                    txtINPUT.Text = "";
                                    e.Handled = true;
                                    return;
                                }
                                else if (tmpcph == null && CurrentEditTrace13List != null && CurrentEditTrace13List.Count > 0)
                                {
                                    var tmpcph13 = CurrentEditTrace13List.Where(a => a.cgoodsid == CurrentEditGoods.cgoodsid && a.cbilid == (CurrentEditGoods.cbilid == "Add" ? CurrentEditGoods.ref_cbilid : CurrentEditGoods.cbilid) && a.cph == ChooseGoodsInfo[0].cph && ChooseGoodsInfo[0].cph == CurrentEditGoods.cph && a.id1 == CurrentEditGoods.id1).FirstOrDefault();
                                    if (iSamePHProcess > 1)
                                    {
                                        tmpcph13 = CurrentEditTrace13List.Where(a => a.cgoodsid == CurrentEditGoods.cgoodsid && a.cbilid == (CurrentEditGoods.cbilid == "Add" ? CurrentEditGoods.ref_cbilid : CurrentEditGoods.cbilid) && a.cph.Contains(ChooseGoodsInfo[0].cph) && CurrentEditGoods.cph == ChooseGoodsInfo[0].cph && a.dmadedate == ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate) && a.dexpdate == ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate) && a.id1 == CurrentEditGoods.id1).FirstOrDefault();
                                    }
                                    if (GlobalDataCache.p_SOFTUSERTYPE == "zz579" && CurrentSHMainInfo.ishtype == 2)
                                    {
                                        ////广州华亘朗博-器械
                                        tmpcph13 = CurrentEditTrace13List.Where(a => a.cgoodsid == CurrentEditGoods.cgoodsid && a.ctracename == ChooseGoodsInfo[0].cudicode && a.cph.Contains(ChooseGoodsInfo[0].cph) && CurrentEditGoods.cph == ChooseGoodsInfo[0].cph && a.dmadedate == tmpcph.dmadedate && a.dexpdate == tmpcph.dexpdate && a.id1 == CurrentEditGoods.id1).FirstOrDefault();
                                    }
                                    if (tmpcph13 != null)
                                    {
                                        //更新数量 
                                        decimal reffqty = CheckYshqty();
                                        if (reffqty - ChooseGoodsInfo[0].fqty <= 0)
                                        {
                                            Toast.MakeText(this, "拒收数量+收货数量要小于或等于通知数量！", 0).Show();
                                            txtINPUT.Text = "";
                                            e.Handled = true;
                                            return;
                                        }
                                        var bresult = false;
                                        var tmp = CoumTracefqty(CurrentEditGoods, "check", ref bresult);
                                        if (!bresult)
                                        {
                                            this.txtSHSL.Text = tmp.ToString(GlobalDataCache.QtyFormat);
                                            SaveLRData(CurrentEditGoods);
                                            if (txtPH.Text != ChooseGoodsInfo[0].cph || txtGOODSNAME.Text != ChooseGoodsInfo[0].cgoodsname)//商品或批号不相同时，重新赋值
                                            {
                                                SetGoodsText(CurrentEditGoods);
                                            }
                                        }
                                        txtINPUT.Text = "";
                                        e.Handled = true;
                                        return;
                                    }
                                    else
                                    {
                                        txtINPUT.Text = ChooseGoodsInfo[0].cgoodsid;
                                    }
                                }

                                else
                                {
                                    txtINPUT.Text = ChooseGoodsInfo[0].cgoodsid;
                                }
                            }
                            else
                            {
                                txtINPUT.Text = ChooseGoodsInfo[0].cgoodsid;
                            }
                        }
                        else if (CurrentEditGoods != null && CurrentEditTrace13List != null && CurrentEditTrace13List.Count > 0 && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && CurrentEditTrace13List.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph == ChooseGoodsInfo[0].cph).Any() && CurrentEditGoods.cgoodsid == ChooseGoodsInfo[0].cgoodsid)
                        {
                            var tmpcph = CurrentEditTrace13List.Where(a => a.cgoodsid == CurrentEditGoods.cgoodsid && a.cbilid == (CurrentEditGoods.cbilid == "Add" ? CurrentEditGoods.ref_cbilid : CurrentEditGoods.cbilid) && a.cph == ChooseGoodsInfo[0].cph && CurrentEditGoods.cph == ChooseGoodsInfo[0].cph && a.id1 == CurrentEditGoods.id1).FirstOrDefault();
                            if (iSamePHProcess > 1)
                            {
                                tmpcph = CurrentEditTrace13List.Where(a => a.cgoodsid == CurrentEditGoods.cgoodsid && a.cbilid == (CurrentEditGoods.cbilid == "Add" ? CurrentEditGoods.ref_cbilid : CurrentEditGoods.cbilid) && a.cph == ChooseGoodsInfo[0].cph && CurrentEditGoods.cph == ChooseGoodsInfo[0].cph && a.dmadedate == ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate) && a.dexpdate == ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate) && a.id1 == CurrentEditGoods.id1).FirstOrDefault();
                            }
                            if (GlobalDataCache.p_SOFTUSERTYPE == "zz579" && CurrentSHMainInfo.ishtype == 2)
                            {
                                ////广州华亘朗博-器械
                                tmpcph = CurrentEditTrace13List.Where(a => a.cgoodsid == CurrentEditGoods.cgoodsid && a.ctracename == ChooseGoodsInfo[0].cudicode && a.cph.Contains(ChooseGoodsInfo[0].cph) && CurrentEditGoods.cph == ChooseGoodsInfo[0].cph && a.dmadedate == CurrentEditGoods.dmadedate && a.dexpdate == CurrentEditGoods.dexpdate && a.id1 == CurrentEditGoods.id1).FirstOrDefault();
                            }
                            if (tmpcph != null)
                            {
                                //更新数量 
                                decimal reffqty = CheckYshqty();
                                if (reffqty - ChooseGoodsInfo[0].fqty <= 0)
                                {
                                    Toast.MakeText(this, "拒收数量+收货数量要小于或等于通知数量！", 0).Show();
                                    txtINPUT.Text = "";
                                    e.Handled = true;
                                    return;
                                }
                                var bresult = false;
                                var tmp = CoumTracefqty(CurrentEditGoods, "check", ref bresult);
                                if (!bresult)
                                {
                                    this.txtSHSL.Text = tmp.ToString(GlobalDataCache.QtyFormat);
                                    SaveLRData(CurrentEditGoods);
                                }
                                txtINPUT.Text = "";
                                e.Handled = true;
                                return;
                            }
                            else if (tmpcph == null && CurrentEditTrace12List != null && CurrentEditTrace12List.Count > 0)
                            {
                                var tmpcph12 = CurrentEditTrace12List.Where(a => a.cgoodsid == CurrentEditGoods.cgoodsid && a.cbilid == (CurrentEditGoods.cbilid == "Add" ? CurrentEditGoods.ref_cbilid : CurrentEditGoods.cbilid) && a.cph == ChooseGoodsInfo[0].cph && CurrentEditGoods.cph == ChooseGoodsInfo[0].cph && a.id1 == CurrentEditGoods.id1).FirstOrDefault();
                                if (iSamePHProcess > 1)
                                {
                                    tmpcph12 = CurrentEditTrace12List.Where(a => a.cgoodsid == CurrentEditGoods.cgoodsid && a.cbilid == (CurrentEditGoods.cbilid == "Add" ? CurrentEditGoods.ref_cbilid : CurrentEditGoods.cbilid) && a.cph == ChooseGoodsInfo[0].cph && CurrentEditGoods.cph == ChooseGoodsInfo[0].cph && a.dmadedate == ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate) && a.dexpdate == ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate) && a.id1 == CurrentEditGoods.id1).FirstOrDefault();
                                }
                                if (GlobalDataCache.p_SOFTUSERTYPE == "zz579" && CurrentSHMainInfo.ishtype == 2)
                                {
                                    ////广州华亘朗博-器械
                                    tmpcph12 = CurrentEditTrace12List.Where(a => a.cgoodsid == CurrentEditGoods.cgoodsid && a.cparenttrace == ChooseGoodsInfo[0].cudicode && a.cph == ChooseGoodsInfo[0].cph && CurrentEditGoods.cph == ChooseGoodsInfo[0].cph && a.dmadedate == tmpcph.dmadedate && a.dexpdate == tmpcph.dexpdate && a.id1 == CurrentEditGoods.id1).FirstOrDefault();
                                }
                                if (tmpcph12 != null)
                                {
                                    //更新数量 
                                    decimal reffqty = CheckYshqty();
                                    if (reffqty - ChooseGoodsInfo[0].fqty <= 0)
                                    {
                                        Toast.MakeText(this, "拒收数量+收货数量要小于或等于通知数量！", 0).Show();
                                        txtINPUT.Text = "";
                                        e.Handled = true;
                                        return;
                                    }
                                    var bresult = false;
                                    var tmp = CoumTracefqty(CurrentEditGoods, "check", ref bresult);
                                    if (!bresult)
                                    {
                                        this.txtSHSL.Text = tmp.ToString(GlobalDataCache.QtyFormat);
                                        SaveLRData(CurrentEditGoods);
                                    }
                                    txtINPUT.Text = "";
                                    e.Handled = true;
                                    return;
                                }
                                else
                                {
                                    txtINPUT.Text = ChooseGoodsInfo[0].cgoodsid;
                                }
                            }
                            else
                            {
                                txtINPUT.Text = ChooseGoodsInfo[0].cgoodsid;
                            }
                        }
                        else if (ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && CurrentEditGoods != null && iSamePHProcess > 1 && ChooseGoodsInfo[0].cgoodsid == CurrentEditGoods.cgoodsid && ChooseGoodsInfo[0].cph == CurrentEditGoods.cph && ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate) == CurrentEditGoods.dmadedate && ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate) == CurrentEditGoods.dexpdate)
                        {
                            //扫描的是组合条码，并且品种批号与当前编辑的一致时，直接更新数量,当前数量加1
                            decimal reffqty = CheckYshqty();
                            if (reffqty - ChooseGoodsInfo[0].fqty <= 0)
                            {
                                Toast.MakeText(this, "拒收数量+收货数量要小于或等于通知数量！", 0).Show();
                                txtINPUT.Text = "";
                                e.Handled = true;
                                return;
                            }
                            this.txtSHSL.Text = (ConvertHelper.ToDecimal(txtSHSL.Text) + ChooseGoodsInfo[0].fqty).ToString(GlobalDataCache.QtyFormat);
                            SaveLRData(CurrentEditGoods);
                            txtINPUT.Text = "";
                            e.Handled = true;
                            return;
                        }
                        else if (ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && CurrentEditGoods != null && iSamePHProcess <= 1 && ChooseGoodsInfo[0].cgoodsid == CurrentEditGoods.cgoodsid && ChooseGoodsInfo[0].cph == CurrentEditGoods.cph)
                        {
                            //扫描的是组合条码，并且品种批号与当前编辑的一致时，直接更新数量,当前数量加1
                            decimal reffqty = CheckYshqty();
                            if (reffqty - ChooseGoodsInfo[0].fqty <= 0)
                            {
                                Toast.MakeText(this, "拒收数量+收货数量要小于或等于通知数量！", 0).Show();
                                txtINPUT.Text = "";
                                e.Handled = true;
                                return;
                            }
                            this.txtSHSL.Text = (ConvertHelper.ToDecimal(txtSHSL.Text) + ChooseGoodsInfo[0].fqty).ToString(GlobalDataCache.QtyFormat);
                            SaveLRData(CurrentEditGoods);
                            txtINPUT.Text = "";
                            e.Handled = true;
                            return;
                        }
                        else if (ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
                        {
                            txtINPUT.Text = ChooseGoodsInfo[0].cgoodsid;
                        }
                        else if (dialogUDIList != null && dialogUDIList.Count > 1)//下拉选择
                        {
                            e.Handled = true;
                            return;
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

        void txtQuery_Click(object sender, EventArgs e)
        {
            if (isPopuChooseForm)
            {
                return;
            }
            isPopuChooseForm = true;
            var txt = txtINPUT.Text.Trim();
            if (string.IsNullOrWhiteSpace(txt))
            {
                if (bchkUDI)
                {
                    Toast.MakeText(this, "没有对应的商品信息！", ToastLength.Long).Show();
                }
                //Toast.MakeText(this, "查询内容不能为空！", ToastLength.Long).Show();
                isPopuChooseForm = false;
                return;
            }

            this.ClearAllControl();
            this.Proxy.Execute(new PDASHOPRequest()
            {
                OPType = 4,
                EmpCode = GlobalProxySetting.GetLoginState().EmployeeCode,
                cbilid = CurrentSHMainInfo.cbilid,
                ccorpid = CurrentSHMainInfo.ccorpid,
                OrgID = GlobalProxySetting.OrgID,
                cckid = CurrentSHMainInfo.cckid,
                ishtype = CurrentSHMainInfo.ishtype,
                QueryText = txt
            }, (response) =>
                              {
                                  if (!response.IsError)
                                  {
                                      dialogList = response.SHGoodsList;
                                      if (GlobalDataCache.p_SOFTUSERTYPE == "zz579" && CurrentSHMainInfo != null && CurrentSHMainInfo.ishtype == 2 && dialogList != null && dialogList.Count > 0 && bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
                                      {
                                          //广州华亘朗博-器械
                                          var dialogListTmp = response.SHGoodsList.Where(a => a.creserve1 == ChooseGoodsInfo[0].cudicode).ToList();
                                          if (dialogListTmp != null && dialogListTmp.Count > 0)
                                          {
                                              dialogList = dialogListTmp;
                                          }
                                      }
                                      if (bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && CurrentSHMainInfo.ishtype == 1 && dialogList.Count() > 1 && strpdashGetddtype != "2")
                                      {
                                          foreach (var item in dialogList)
                                          {
                                              var fshqty = 0M;
                                              if (CurrentEditGoodsList != null)
                                              {
                                                  var ysshlist = CurrentEditGoodsList.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cgoodsid == item.cgoodsid).ToList();
                                                  if (ysshlist != null && ysshlist.Count > 0)//KB023 2023-06-19 校验数量修改，避免出现每次批号不一样，数量校验不正确问题
                                                  {
                                                      for (int i = 0; i < ysshlist.Count; i++)
                                                      {
                                                          if (ysshlist[i].PHList.Count > 0)
                                                          {
                                                              fshqty += ysshlist[i].PHList.Sum(p => p.fshqty + p.fqsqty);
                                                          }
                                                      }
                                                  }
                                              }
                                              if (dialogList.Where(p => p.cgoodsid == ChooseGoodsInfo[0].cgoodsid && p.fqty >= ChooseGoodsInfo[0].fqty + fshqty).Count() > 1)
                                              {
                                                  var tmpdialog = dialogList.Where(p => p.cgoodsid == ChooseGoodsInfo[0].cgoodsid && p.fqty >= ChooseGoodsInfo[0].fqty + fshqty).ToList().FirstOrDefault();
                                                  if (strpdashGetddtype == "1")
                                                  {
                                                      tmpdialog = dialogList.Where(p => p.cgoodsid == ChooseGoodsInfo[0].cgoodsid && p.fqty >= ChooseGoodsInfo[0].fqty + fshqty).OrderBy(d => d.dbildate).ToList().FirstOrDefault();
                                                  }
                                                  dialogList.Clear();
                                                  dialogList.Add(tmpdialog);
                                                  break;
                                              }
                                              if (item.fqty > 0 && dialogList.Where(p => p.cgoodsid == ChooseGoodsInfo[0].cgoodsid && p.fqty >= ChooseGoodsInfo[0].fqty).Count() == 0)
                                              {
                                                  if (dialogList.Where(p => p.cgoodsid == ChooseGoodsInfo[0].cgoodsid).Count() == 0)
                                                  {
                                                      Toast.MakeText(this, "已没有订单可选。", 0).Show();
                                                  }
                                                  else
                                                  {
                                                      Toast.MakeText(this, "请注意，剩余的订单数量都小于条码解析的数量。", 0).Show();
                                                  }
                                                  return;
                                              }
                                          }
                                      }
                                      if (bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && CurrentSHMainInfo.ishtype != 1 && dialogList.Count() > 1)
                                      {
                                          if (iSamePHProcess > 1)
                                          {
                                              if (!string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].dmadedate) && !string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].dexpdate))
                                              {
                                                  dialogList = dialogList.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph.Contains(ChooseGoodsInfo[0].cph) && a.dmadedate == ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate) && a.dexpdate == ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate)).ToList();
                                              }
                                              else if (string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].dmadedate) && !string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].dexpdate))
                                              {
                                                  dialogList = dialogList.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph.Contains(ChooseGoodsInfo[0].cph) && a.dexpdate == ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate)).ToList();
                                              }
                                              else if (!string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].dmadedate) && string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].dexpdate))
                                              {
                                                  dialogList = dialogList.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph.Contains(ChooseGoodsInfo[0].cph) && a.dmadedate == ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate)).ToList();
                                              }
                                          }
                                          else
                                          {
                                              dialogList = dialogList.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph == ChooseGoodsInfo[0].cph).ToList();
                                          }

                                      }
                                      if (bchkUDI && CurrentEditGoods != null && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0 && this.btnNewPH.Text == "完成")//未完成状态
                                      {
                                          if (iSamePHProcess > 1)
                                          {
                                              if (CurrentEditGoods.cgoodsid != ChooseGoodsInfo[0].cgoodsid || CurrentEditGoods.cph != ChooseGoodsInfo[0].cph || txtSCDATE.Text != ConvertHelper.ToString(ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate)) || txtYXDATE.Text != ConvertHelper.ToString(ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate)))
                                              {
                                                  CustomAlertDialog customAlertDialog = new CustomAlertDialog();
                                                  customAlertDialog.OKClick += CustomAlertDialog_SHOKClick;
                                                  customAlertDialog.CancelClick += CustomAlertDialog_SHCalcelClick;
                                                  //txtdetail.Text = "是否删除批号【" + this.CurrentEditGoodPHInfo.cph + "】？";
                                                  customAlertDialog.AlertDialogShow(this, "当前收货与录入的收货信息不一致，是否完成已录入的收货商品信息【" + CurrentEditGoods.cph + "】？");
                                              }
                                          }
                                          else
                                          {
                                              if (CurrentEditGoods.cgoodsid != ChooseGoodsInfo[0].cgoodsid || CurrentEditGoods.cph != ChooseGoodsInfo[0].cph)
                                              {
                                                  CustomAlertDialog customAlertDialog = new CustomAlertDialog();
                                                  customAlertDialog.OKClick += CustomAlertDialog_SHOKClick;
                                                  customAlertDialog.CancelClick += CustomAlertDialog_SHCalcelClick;
                                                  //txtdetail.Text = "是否删除批号【" + this.CurrentEditGoodPHInfo.cph + "】？";
                                                  customAlertDialog.AlertDialogShow(this, "当前收货与录入的收货信息不一致，是否完成已录入的收货商品信息【" + CurrentEditGoods.cph + "】？");
                                              }
                                          }
                                      }
                                      else
                                      {
                                          if (dialogList.Count() > 1)
                                          {
                                              popWindowAdapter = new ListViewPopWindowAdapter(this, dialogList, this);
                                              popWindowAdapter.SetOnDismissListener(this);
                                              popWindowAdapter.Width = txtINPUT.Width;
                                              popWindowAdapter.ShowAsDropDown(txtINPUT);
                                              //popWindowAdapter.ShowAtLocation(this.Window.DecorView, GravityFlags.Top, 0, 0);
                                          }
                                          else if (dialogList.Count() == 1)
                                          {
                                              SetControlText(dialogList[0]);
                                          }
                                          else
                                          {
                                              SetControlText(null);
                                              Toast.MakeText(this, "没有对应的商品信息！", 0).Show();
                                          }
                                      }

                                  }
                                  if (chkUDI != null && chkUDI.Visibility == ViewStates.Visible)
                                  {
                                      if (CurrentEditTrace12List == null) CurrentEditTrace12List = response.LRInfoTrace12List;
                                      if (CurrentEditTrace13List == null) CurrentEditTrace13List = response.LRInfoTrace13List;
                                  }

                                  isPopuChooseForm = false;
                              }, this);


        }

        public void OnItemClick(AdapterView parent, View view, int position, long id)
        {
            if (dialogUDIList != null && dialogUDIList.Count > 1 && ChooseGoodsInfo != null && ChooseGoodsInfo.Count < 1)
            {
                popUDIWindowAdapter.Dismiss();
                DataRow UDISel = dialogUDIList[position];
                if (bchkUDI)
                {
                    InstrumentGoodsInfo goodsInfo = null;
                    goodsInfo = DataTableHelper.DataRowToClass<InstrumentGoodsInfo>(UDISel);
                    ChooseGoodsInfo.Add(goodsInfo);
                    txtINPUT.Text = ChooseGoodsInfo[0].cgoodsid;
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
                txtINPUT.Text = infoQr.cgoodsid;
                txtQuery_Click(null, null);
                dialogQRList = null;
            }
            else if (mtype == 1)
            {
                spinnerPopcfileno.Dismiss();
                if (cfilenogoodfilelist != null && cfilenogoodfilelist.Count > 0)
                {
                    txtFILE.Text = cfilenogoodfilelist[position].ToString();
                    //cfilenogoodfilelist = null;
                    mtype = 0;
                }
            }
            else
            {
                popWindowAdapter.Dismiss();
                SetControlText(dialogList[position]);
            }
        }

        #endregion

        /// <summary>
        /// popupWindow取消
        /// </summary>
        public void OnDismiss()
        {

            //SetTextImage(Resource.Drawable.spinner);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Android.App.Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            InitCurrentEditGoodsList();
        }

        #region 页面数据处理

        private void InitCurrentEditGoodsList()
        {
            if (CurrentSHMainInfo.cbilid == "Add" && string.IsNullOrEmpty(GlobalDataCache.GetData<string>("temp_sh_cbilid")))
            {
                GlobalDataCache.SetData("SHEditGoodsList", CurrentEditGoodsList);
                return;
            }
            else if (!string.IsNullOrEmpty(GlobalDataCache.GetData<string>("temp_sh_cbilid")))
            {
                CurrentSHMainInfo.cbilid = GlobalDataCache.GetData<string>("temp_sh_cbilid");
            }


            this.Proxy.Execute(new PDASHOPRequest()
            {
                OPType = 8,
                EmpCode = GlobalProxySetting.GetLoginState().EmployeeCode,
                cbilid = CurrentSHMainInfo.cbilid
            }, (response) =>
            {
                if (!response.IsError)
                {
                    CurrentEditGoodsList = response.SHGoodsList;
                    if (chkUDI != null && chkUDI.Visibility == ViewStates.Visible)
                    {
                        if (CurrentEditTrace12List != null)
                        {
                            CurrentEditTrace12List = response.LRInfoTrace12List;
                        }
                        if (CurrentEditTrace13List != null)
                        {
                            CurrentEditTrace13List = response.LRInfoTrace13List;
                        }
                    }
                    GlobalDataCache.SetData("SHEditGoodsList", CurrentEditGoodsList);
                }
            }, this);
        }

        private void RefreshCurrentEditGoodsList()
        {
            this.ColdStore = GlobalDataCache.GetData<ColdStoreInfo>("SHColdStoreInfo");
            foreach (var item in CurrentEditGoodsList)
            {
                if (item.isgspcold == 1)
                {
                    item.csendadd = ColdStore.csendadd;
                    item.dsendstart = ColdStore.dsendstart;
                    item.drectime = ColdStore.drectime;
                    item.fhours = ColdStore.fhours;
                    item.ctep = ColdStore.ctep;
                    item.csendtype = ColdStore.csendtype;
                    item.ctepctl = ColdStore.ctepctl;
                    item.ctepnote = ColdStore.ctepnote;
                    item.ctepsta = ColdStore.ctepsta;
                    item.ccar = ColdStore.ccar;
                    item.ccarno = ColdStore.ccarno;
                    item.csender = ColdStore.csender;
                    item.ctranser = ColdStore.ctranser;
                    item.ctransport = ColdStore.ctransport;
                    item.cdap = ColdStore.cdap;
                    item.cstep = ColdStore.cstep;
                    item.csdap = ColdStore.csdap;
                }
            }
        }

        private void ClearEditData()
        {
            SetControlText(null);
            if (CurrentEditGoodsList != null && CurrentEditGoodsList.Count > 0)
            {
                CurrentEditGoodsList.Clear();
            }
            if (CurrentEditTrace12List != null && CurrentEditTrace12List.Count > 0)
            {
                CurrentEditTrace12List.Clear();
            }
            if (CurrentEditTrace13List != null && CurrentEditTrace13List.Count > 0)
            {
                CurrentEditTrace13List.Clear();
            }

            CurrentEditGoods = new SHGoods();
            CurrentEditGoods.PHList = new List<SHPHInfo>();
            listView.Adapter = new SHLRGoodsInfoAdapter(this, CurrentEditGoods.PHList);
            //if (CurrentEditGoods.PHList.Count() == 0)
            //{
            //    BtnNewPH_Click(null, null);
            //}

            //ColdStore = null;
        }

        #endregion

        #region UDI码相关处理

        private bool DealSCanRQCode()
        {
            bool bResult = false;
            var txt = txtINPUT.Text.Trim();
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
                    popUDIWindowAdapter.ShowAsDropDown(txtINPUT);
                    bResult = true;
                }
                else
                {
                    infoQr = info;
                    txtINPUT.Text = infoQr.cgoodsid;
                    bResult = true;
                }
            }
            return bResult;
        }
        private async Task<bool> DealUDICodeAsync()
        {
            var txt = txtINPUT.Text.Trim();
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
                foreach (var item in CurrentEditGoodsList)
                {
                    var dic = new Dictionary<string, string>();
                    //case 0: return ConvertHelper.ToString(row["cgoodsid"]);
                    //case 1: return ConvertHelper.ToString(row["cgoodsid_v_cgoodsname"]);
                    dic["cgoodsid"] = item.cgoodsid;
                    dic["cgoodsid_v_cgoodsname"] = item.cgoodsname;
                    d1.Add(dic);
                }
                (UDIGoodsInfo, UDIlastCode,  UDIastUDIRuleLen,  UDIRulecode,  _ErrMsg) = await DataTableHelper.GetUdiInstorCodeExAsync(txt, _Rulecode, _LastUDIgoodsid,  UDIlastCode,  UDIastUDIRuleLen,  UDIRulecode,  _ErrMsg, fz_chk_sel.Checked,d1,this);
                if (!string.IsNullOrWhiteSpace(_ErrMsg))
                {
                    Toast.MakeText(this, _ErrMsg, ToastLength.Long).Show();
                    isPopuChooseForm = false;
                    this.txtINPUT.Text = "";
                    return bResult;
                }
                if (UDIGoodsInfo != null && UDIGoodsInfo.Count > 0)
                {
                    if (UDIGoodsInfo.Count > 1)//多个弹框选择
                    {
                        var udidt = DataTableHelper.ToDataTabel(UDIGoodsInfo);

                        dialogUDIList = udidt.Select("").ToList();
                        if (GlobalDataCache.p_SOFTUSERTYPE == "C07129")//KB023 2023-04-19  深圳宝原 定制修改
                        {
                            popUDIWindowAdapter = new CommonPopwindowAdapter<DataRow>(this, dialogUDIList, this, Resource.Layout.UDIListDetailPopWindow2);
                            popUDIWindowAdapter.SetListViewColumn += (position, convertView, parent, row, viewHolder) =>
                            {
                                viewHolder.SetText(Resource.Id.textView1, row["cgoodsname"].ToString() + "\\" + row["cgoodsid"].ToString());//+ "\\"+row["cgoodsid"].ToString()
                                viewHolder.SetText(Resource.Id.textView2, row["cpkname"].ToString());
                                viewHolder.SetText(Resource.Id.textView3, row["cph"].ToString());
                                return viewHolder.GetConvertView();
                            };
                        }
                        else
                        {
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
                        }
                        popUDIWindowAdapter.SetOnDismissListener(this);
                        //popWindowAdapter.Width = txtcBarcode.Width;
                        popUDIWindowAdapter.ShowAsDropDown(txtINPUT);
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
                    this.txtINPUT.Text = "";
                    _lastCode = UDIlastCode;
                    return bResult;
                }
                else
                {
                    Toast.MakeText(this, "未解析出条码规则或条码规则解析错误！", ToastLength.Long).Show();
                    isPopuChooseForm = false;
                    this.txtINPUT.Text = "";
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

        #endregion

        #region 页面信息赋值

        //清空商品信息控件值
        private void ClearEditControl()
        {
            this.txtINPUT.Text = "";
            this.txtPH.Text = "";
            this.txtYXDATE.Text = "";
            this.txtSCDATE.Text = "";

            spSeason.SetSelection(rejectionReasons.Count() - 1);
        }

        private void ClearAllControl()
        {
            this.txtINPUT.Text = "";
            this.txtGOODSNAME.Text = "";
            this.lblDW.Text = "";
            this.txtGG.Text = "";
            this.txtBARCODE.Text = "";
            this.txtFILE.Text = "";
            this.txtFACTORYNAME.Text = "";
            this.txtPH.Text = "";
            this.txtSCDATE.Text = "";
            this.txtSHSL.Text = "";
            this.txtYXDATE.Text = "";
            this.txtQSSL.Text = "";
            this.txt_refcbilid.Text = "";
            this.txt_scxkz.Text = "";
            spSeason.SetSelection(rejectionReasons.Count() - 1);

            txtINPUT.Focusable = true;
            txtINPUT.FocusableInTouchMode = true;
            txtINPUT.RequestFocus();
        }

        private void SetGoodsText(SHGoods info)
        {
            txtGOODSNAME.Text = info.cgoodsname + "(" + info.cgoodsid + ")";
            lblDW.Text = info.cunit;
            txtGG.Text = info.cpkname;
            txtBARCODE.Text = info.cbarcode;
            txt_notice_fqty.Text = info.ref_fqty.ToString(GlobalDataCache.QtyFormat);
            txtFILE.Text = string.IsNullOrWhiteSpace(info.cphnote2) ? info.cfileno : info.cphnote2;
            txtFACTORYNAME.Text = info.cfactoryname;
            txtSHSL.Text = info.fqty.ToString(GlobalDataCache.QtyFormat);
            txtQSSL.Text = info.frecrejectqty.ToString(GlobalDataCache.QtyFormat);
            txt_hw.Text = info.chwcode;
            txt_refcbilid.Text = info.ref_cbilid;
            txt_scxkz.Text = info.ccertificateno;
            //txt_scxkz.Text = info.cfileno; //V1.2 这个不是生产许可证号，这个是批准文号
            FindViewById<TextView>(Resource.Id.tv_wms).Text = string.Format("{0}*{1}*{2}",
                ConvertHelper.ToDecimalFormatString(info.fpklong),
                ConvertHelper.ToDecimalFormatString(info.fpkwidth),
                ConvertHelper.ToDecimalFormatString(info.fpkheight));

            this.txtSCDATE.Enabled = false;
            this.txtYXDATE.Enabled = false;
            this.txtPH.Enabled = false;
            this.txtSHSL.Enabled = false;

            this.btnDelPH.Enabled = true;
            this.btnNewPH.Enabled = true;
            if (IsEnableSFDARenewal)
            {
                cfilenofilelist = info.cfilenofilelist;
                if (cfilenofilelist != null && cfilenofilelist.Count > 0)
                {
                    addcfileno(info.cgoodsid);
                }
            }
            else
            {

            }
            this.ClearEditControl();
        }

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
                mtype = 1;
                spinnerPopcfileno.Width = txtFILE.Width;
                spinnerPopcfileno.ShowAsDropDown(txtFILE);
                //SetTextImage(Resource.Drawable.icon_up);
            };
        }

        private void SetControlText(SHGoods info)
        {
            this.ClearAllControl();
            if (info != null)
            {
                //还原本单的通知数量
                //if(CurrentEditGoodsList.Any(a => a.id1 == CurrentEditGoods.id1 && a.ref_cbilid != a.cbilid))
                //{
                //    var ref_fqty = CurrentEditGoodsList.FirstOrDefault(a => a.id1 == CurrentEditGoods.id1 && a.ref_cbilid != a.cbilid).ref_fqty;
                //    CurrentEditGoods.ref_fqty = ref_fqty;
                //}
                //else
                //{
                //    CurrentEditGoods.ref_fqty = CurrentEditGoods.ref_fqty + CurrentEditGoodsList.Sum(a => a.fqty);
                //}

                //V1.4 改不动
                if (!tempRefQty.ContainsKey(info.cgoodsid))
                {
                    var qty = info.ref_fqty + CurrentEditGoodsList.Where(g=>g.cgoodsid == info.cgoodsid).Sum(a => a.fqty);
                    //var qty = info.ref_fqty + CurrentEditGoodsList.Sum(a => a.fqty);
                    tempRefQty[info.cgoodsid] = qty;
                    
                }
                info.ref_fqty = tempRefQty[info.cgoodsid];

                this.SetGoodsText(info);
                //计算本单中在来源单中总共能收多少
                //if()
                //info.ref_fqty = info.ref_fqty + CurrentEditGoodsList.Sum(a => a.fqty);
                if (iSamePHProcess > 1 && CurrentEditGoodsList.Any(p => p.cgoodsid == info.cgoodsid && p.ref_cbilid == info.ref_cbilid && p.cph == info.cph && p.dmadedate == info.dmadedate && p.dexpdate == info.dexpdate))
                {
                    CurrentEditGoods = CurrentEditGoodsList.Where(p => p.cgoodsid == info.cgoodsid && p.ref_cbilid == info.ref_cbilid && p.cph == info.cph && p.dmadedate == info.dmadedate && p.dexpdate == info.dexpdate).FirstOrDefault();

                    CurrentEditGoodPHInfoList = info.PHList;
                    if (GlobalDataCache.p_SOFTUSERTYPE == "zz579" && CurrentSHMainInfo != null && CurrentSHMainInfo.ishtype == 2 && CurrentEditGoods.id1 != info.id1 && bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
                    {
                        //KB023 2024-03-27修改
                        CurrentEditGoods = info;
                        CurrentEditGoods.PHList = new List<SHPHInfo>(); //清除返回的批号列表
                        if (this.txtPH.Text == "") this.txtPH.Text = CurrentEditGoods.cph;
                        if (this.txtSCDATE.Text == "") this.txtSCDATE.Text = CurrentEditGoods.dmadedate;
                        if (this.txtSHSL.Text == "") this.txtSHSL.Text = ChooseGoodsInfo[0].fqty.ToString(GlobalDataCache.QtyFormat);
                        if (this.txtYXDATE.Text == "") this.txtYXDATE.Text = CurrentEditGoods.dexpdate;

                    }
                }
                else if (iSamePHProcess <= 1 && CurrentEditGoodsList.Any(p => p.cgoodsid == info.cgoodsid && p.ref_cbilid == info.ref_cbilid && p.cph == info.cph))
                {
                    CurrentEditGoods = CurrentEditGoodsList.Where(p => p.cgoodsid == info.cgoodsid && p.ref_cbilid == info.ref_cbilid && p.cph == info.cph).FirstOrDefault();
                    CurrentEditGoodPHInfoList = info.PHList;
                    if (GlobalDataCache.p_SOFTUSERTYPE == "zz579" && CurrentSHMainInfo != null && CurrentSHMainInfo.ishtype == 2 && CurrentEditGoods.id1 != info.id1 && bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
                    {
                        //KB023 2024-03-27修改
                        CurrentEditGoods = info;
                        CurrentEditGoods.PHList = new List<SHPHInfo>(); //清除返回的批号列表
                        if (this.txtPH.Text == "") this.txtPH.Text = CurrentEditGoods.cph;
                        if (this.txtSCDATE.Text == "") this.txtSCDATE.Text = CurrentEditGoods.dmadedate;
                        if (this.txtSHSL.Text == "") this.txtSHSL.Text = ChooseGoodsInfo[0].fqty.ToString(GlobalDataCache.QtyFormat);
                        if (this.txtYXDATE.Text == "") this.txtYXDATE.Text = CurrentEditGoods.dexpdate;

                    }
                }
                else
                {
                    CurrentEditGoods = info;
                    if (this.txtPH.Text == "")
                    {
                        if (bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
                        {
                            this.txtPH.Text = ChooseGoodsInfo[0].cph;
                        }
                        else
                        {
                            this.txtPH.Text = CurrentEditGoods.cph;
                        }
                    }
                    if (this.txtSCDATE.Text == "")
                    {
                        if (bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
                        {
                            this.txtSCDATE.Text = (!string.IsNullOrWhiteSpace(ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate)) ? ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate) : "");
                        }
                        else
                        {
                            this.txtSCDATE.Text = CurrentEditGoods.dmadedate;
                        }
                    }
                    if (this.txtSHSL.Text == "")
                    {
                        if (bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
                        {
                            this.txtSHSL.Text = CurrentEditGoods.fqty.ToString(GlobalDataCache.QtyFormat);
                        }
                        else
                        {
                            this.txtSHSL.Text = ChooseGoodsInfo[0].fqty.ToString(GlobalDataCache.QtyFormat);
                        }
                    }
                    if (this.txtYXDATE.Text == "")
                    {
                        if (bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
                        {
                            this.txtYXDATE.Text = (!string.IsNullOrWhiteSpace(ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate)) ? ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate) : "");
                        }
                        else
                        {
                            this.txtYXDATE.Text = CurrentEditGoods.dexpdate;
                        }
                    }

                    ModifyGoodsColdInfo(CurrentEditGoods, ColdStore);
                    CurrentEditGoodPHInfoList = info.PHList;
                    CurrentEditGoods.PHList = new List<SHPHInfo>(); //清除返回的批号列表
                }
                listView.Adapter = new SHLRGoodsInfoAdapter(this, CurrentEditGoods.PHList);
                if (CurrentEditGoods.PHList.Count() == 0)
                {
                    BtnNewPH_Click(null, null);
                }

            }
            GC.Collect();
        }

        private void ModifyGoodsColdInfo(SHGoods item, ColdStoreInfo coldStore)
        {
            if (item.isgspcold == 1)
            {
                item.csendadd = coldStore.csendadd;
                item.dsendstart = coldStore.dsendstart;
                item.drectime = coldStore.drectime;
                item.fhours = coldStore.fhours;
                item.ctep = coldStore.ctep;
                item.csendtype = coldStore.csendtype;
                item.ctepctl = coldStore.ctepctl;
                item.ctepnote = coldStore.ctepnote;
                item.ctepsta = coldStore.ctepsta;
                item.ccar = coldStore.ccar;
                item.ccarno = coldStore.ccarno;
                item.csender = coldStore.csender;
                item.ctranser = coldStore.ctranser;
                item.ctransport = coldStore.ctransport;
                item.cdap = coldStore.cdap;
            }
        }

        #endregion

        #region 录入数据保存

        private void SaveLRData(SHGoods info, string btnText = "")
        {
            if (info == null || string.IsNullOrWhiteSpace(info.cgoodsid))
            {
                Toast.MakeText(this, "商品编码不能为空！", 0).Show();
                return;
            }
            /*更新本地操作的商品明细记录数据*/
            if (iSamePHProcess > 1 && CurrentEditGoodsList.Any(p => p.cgoodsid == info.cgoodsid && p.ref_cbilid == info.ref_cbilid && p.cph == info.cph && p.dmadedate == info.dmadedate && p.dexpdate == info.dexpdate))
            {
                if (info.PHList.Count == 0)
                {
                    //去掉批号为空的数据，否则已收会多显示一条数据。
                    CurrentEditGoodsList.Remove(info);
                }
                else
                {
                    for (int i = 0; i < CurrentEditGoodsList.Count(); i++)
                    {
                        if (GlobalDataCache.p_SOFTUSERTYPE == "zz579" && CurrentSHMainInfo != null && CurrentSHMainInfo.ishtype == 2 && bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
                        {
                            if (CurrentEditGoodsList[i].cgoodsid == info.cgoodsid && CurrentEditGoodsList[i].ref_cbilid == info.ref_cbilid && CurrentEditGoodsList[i].cph == info.cph && CurrentEditGoodsList[i].id1 == info.id1 && CurrentEditGoodsList[i].dmadedate == info.dmadedate && CurrentEditGoodsList[i].dexpdate == info.dexpdate)
                            {
                                CurrentEditGoodsList[i] = info;
                                break;
                            }
                        }
                        else
                        {
                            if (CurrentEditGoodsList[i].cgoodsid == info.cgoodsid && CurrentEditGoodsList[i].ref_cbilid == info.ref_cbilid && CurrentEditGoodsList[i].cph == info.cph && CurrentEditGoodsList[i].dmadedate == info.dmadedate && CurrentEditGoodsList[i].dexpdate == info.dexpdate)
                            {
                                CurrentEditGoodsList[i] = info;
                                break;
                            }
                        }
                    }
                }
                if (GlobalDataCache.p_SOFTUSERTYPE == "zz579" && CurrentSHMainInfo != null && CurrentSHMainInfo.ishtype == 2 && bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)//KB023 2024-03-27 
                {
                    if (!CurrentEditGoodsList.Any(p => p.cgoodsid == info.cgoodsid && p.ref_cbilid == info.ref_cbilid && p.cph == info.cph && p.id1 == info.id1 && p.dmadedate == info.dmadedate && p.dexpdate == info.dexpdate))
                    {
                        CurrentEditGoodsList.Add(info);
                    }
                }
            }
            else if (iSamePHProcess <= 1 && CurrentEditGoodsList.Any(p => p.cgoodsid == info.cgoodsid && p.ref_cbilid == info.ref_cbilid && p.cph == info.cph))
            {
                if (info.PHList.Count == 0)
                {
                    //去掉批号为空的数据，否则已收会多显示一条数据。
                    CurrentEditGoodsList.Remove(info);
                }
                else
                {
                    for (int i = 0; i < CurrentEditGoodsList.Count(); i++)
                    {
                        if (GlobalDataCache.p_SOFTUSERTYPE == "zz579" && CurrentSHMainInfo != null && CurrentSHMainInfo.ishtype == 2 && bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
                        {
                            if (CurrentEditGoodsList[i].cgoodsid == info.cgoodsid && CurrentEditGoodsList[i].ref_cbilid == info.ref_cbilid && CurrentEditGoodsList[i].cph == info.cph && CurrentEditGoodsList[i].id1 == info.id1)
                            {
                                CurrentEditGoodsList[i] = info;
                                break;
                            }
                        }
                        else
                        {
                            if (CurrentEditGoodsList[i].cgoodsid == info.cgoodsid && CurrentEditGoodsList[i].ref_cbilid == info.ref_cbilid && CurrentEditGoodsList[i].cph == info.cph)
                            {
                                CurrentEditGoodsList[i] = info;
                                break;
                            }
                        }
                    }
                }
                if (GlobalDataCache.p_SOFTUSERTYPE == "zz579" && CurrentSHMainInfo != null && CurrentSHMainInfo.ishtype == 2 && bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)//KB023 2024-03-27 
                {
                    if (!CurrentEditGoodsList.Any(p => p.cgoodsid == info.cgoodsid && p.ref_cbilid == info.ref_cbilid && p.cph == info.cph && p.id1 == info.id1))
                    {
                        CurrentEditGoodsList.Add(info);
                    }
                }
            }
            else
            {
                CurrentEditGoodsList.Add(info);
            }
            if (!(btnText == "完成"))
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
                    if (CurrentEditTrace12List != null)
                    {
                        this.txtSHSL.Text = CurrentEditTrace12List.Where(p => p.cgoodsid == info.cgoodsid && p.cph == info.cph && p.dmadedate == info.dmadedate && p.dexpdate == info.dexpdate && p.id1 == info.id1).Sum(d12 => d12.fzsmqty).ToString(GlobalDataCache.QtyFormat);
                    }
                }
                else
                {
                    AddCurrentEditTraceList(info);
                }


            }
            //if (GlobalDataCache.p_SOFTUSERTYPE == "zz579") BtnNewPH_Click(null, null);
            //GlobalDataCache.SetData("SHEditGoodsList", CurrentEditGoodsList);
        }

        private void AddCurrentEditTraceList(SHGoods info)
        {
            if (bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
            {
                if (CurrentEditTrace12List == null) CurrentEditTrace12List = new List<GoodsTrace12>();
                if (CurrentEditTrace13List == null) CurrentEditTrace13List = new List<GoodsTrace13>();

                //if (GlobalProxySetting.ConfigList.Any(r=>r.cparmname == "UseZsmNewProcess" && r.cparmvalue == "0"))
                //{
                if (ChooseGoodsInfo[0].itype == 1)////UDI码处理
                {
                    if (!string.IsNullOrWhiteSpace(ChooseGoodsInfo[0].cudicode))
                    {
                        #region d13记录UDI码
                        var trace13 = new GoodsTrace13();
                        trace13.cbilid = (info.cbilid == "Add" ? info.ref_cbilid : info.cbilid);
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
                        trace13.cph = (iSamePHProcess > 1 && CurrentSHMainInfo.ishtype != 1 ? info.cph : ChooseGoodsInfo[0].cph);
                        trace13.dmadedate = (iSamePHProcess > 1 && CurrentSHMainInfo.ishtype != 1 ? info.dmadedate : ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate));
                        trace13.dexpdate = (iSamePHProcess > 1 && CurrentSHMainInfo.ishtype != 1 ? info.dexpdate : ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate));
                        trace13.cmjph = "";
                        trace13.ctracename = ChooseGoodsInfo[0].cudicode;
                        trace13.cirter = GlobalProxySetting.GetLoginState().EmployeeCode;
                        trace13.falotqty = ChooseGoodsInfo[0].fqty;
                        trace13.cnote = info.ref_cbilid;
                        //2023-05-06 fmtp、fltp、fqty、cbzjb
                        trace13.fmtp = ChooseGoodsInfo[0].fmtp;
                        trace13.fltp = ChooseGoodsInfo[0].fltp;
                        trace13.fqty = ChooseGoodsInfo[0].fqty;
                        trace13.cbzjb = ChooseGoodsInfo[0].cbzjb;
                        CurrentTempEditTrace13 = trace13;
                        if (CurrentEditTrace13List != null && CurrentEditTrace13List.Any(p => p.cgoodsid == ChooseGoodsInfo[0].cgoodsid && p.cph == trace13.cph && p.ctracename == ChooseGoodsInfo[0].cudicode))
                        {
                            if (info.PHList.Count == 0)
                            {
                                //去掉批号为空的数据，否则已收会多显示一条数据。
                                CurrentEditTrace13List.Remove(trace13);
                            }
                            else
                            {
                                for (int i = 0; i < CurrentEditTrace13List.Count(); i++)
                                {
                                    if (CurrentEditTrace13List[i].cgoodsid == ChooseGoodsInfo[0].cgoodsid && CurrentEditTrace13List[i].cph == trace13.cph && CurrentEditTrace13List[i].ctracename == ChooseGoodsInfo[0].cudicode)
                                    {
                                        CurrentEditTrace13List[i] = trace13;
                                        CurrentTempEditTrace13 = trace13;
                                        break;
                                    }
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
                                trace12.cph = (iSamePHProcess > 1 && CurrentSHMainInfo.ishtype != 1 ? info.cph : ChooseGoodsInfo[0].cph);
                                trace12.dmadedate = (iSamePHProcess > 1 && CurrentSHMainInfo.ishtype != 1 ? info.dmadedate : ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate));
                                trace12.dexpdate = (iSamePHProcess > 1 && CurrentSHMainInfo.ishtype != 1 ? info.dexpdate : ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate));
                                trace12.cmjph = "";
                                trace12.ctracename = child;
                                trace12.cdicode = ChooseGoodsInfo[0].ctracecode;
                                trace12.cirter = GlobalProxySetting.GetLoginState().EmployeeCode;
                                trace12.cparenttrace = ChooseGoodsInfo[0].cudicode;
                                trace12.cdicode = ChooseGoodsInfo[0].ctracecode;
                                trace12.fzsmqty = ChooseGoodsInfo[0].fqty;
                                trace12.corgid = CurrentSHMainInfo.corgid;
                                trace12.cnote = info.ref_cbilid;
                                if (CurrentEditTrace12List != null && CurrentEditTrace12List.Any(p => p.cgoodsid == ChooseGoodsInfo[0].cgoodsid && p.cph == trace12.cph && p.ctracename == child && p.cparenttrace == ChooseGoodsInfo[0].cudicode))
                                {
                                    if (info.PHList.Count == 0)
                                    {
                                        //去掉批号为空的数据，否则已收会多显示一条数据。
                                        CurrentEditTrace12List.Remove(trace12);
                                    }
                                    else
                                    {
                                        for (int i = 0; i < CurrentEditTrace12List.Count(); i++)
                                        {
                                            if (CurrentEditTrace12List[i].cgoodsid == ChooseGoodsInfo[0].cgoodsid && CurrentEditTrace12List[i].cph == trace12.cph && CurrentEditTrace12List[i].ctracename == child && CurrentEditTrace12List[i].cparenttrace == ChooseGoodsInfo[0].cudicode)
                                            {
                                                CurrentEditTrace12List[i] = trace12;
                                                CurrentTempEditTrace12 = trace12;
                                                break;
                                            }
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
                                trace12.cph = (iSamePHProcess > 1 && CurrentSHMainInfo.ishtype != 1 ? info.cph : ChooseGoodsInfo[0].cph);
                                trace12.dmadedate = (iSamePHProcess > 1 && CurrentSHMainInfo.ishtype != 1 ? info.dmadedate : ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate));
                                trace12.dexpdate = (iSamePHProcess > 1 && CurrentSHMainInfo.ishtype != 1 ? info.dexpdate : ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate));
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
                                trace12.corgid = CurrentSHMainInfo.corgid;
                                trace12.cnote = info.ref_cbilid;
                                CurrentTempEditTrace12 = trace12;
                                if (CurrentEditTrace12List != null && CurrentEditTrace12List.Any(p => p.cgoodsid == ChooseGoodsInfo[0].cgoodsid && p.cph == trace12.cph && p.cparenttrace == ChooseGoodsInfo[0].cudicode))
                                {
                                    if (info.PHList.Count == 0)
                                    {
                                        //去掉批号为空的数据，否则已收会多显示一条数据。
                                        CurrentEditTrace12List.Remove(trace12);
                                    }
                                    if (CurrentEditTrace12List != null && CurrentEditTrace12List.Count > 0)
                                    {
                                        for (int i = 0; i < CurrentEditTrace12List.Count(); i++)
                                        {
                                            if (CurrentEditTrace12List[i].cgoodsid == ChooseGoodsInfo[0].cgoodsid && CurrentEditTrace12List[i].cph == trace12.cph && CurrentEditTrace12List[i].cparenttrace == ChooseGoodsInfo[0].cudicode)
                                            {
                                                CurrentEditTrace12List[i] = trace12;
                                                CurrentTempEditTrace12 = trace12;
                                                break;
                                            }
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
                        trace12.cph = (iSamePHProcess > 1 && CurrentSHMainInfo.ishtype != 1 ? info.cph : ChooseGoodsInfo[0].cph);
                        trace12.dmadedate = (iSamePHProcess > 1 && CurrentSHMainInfo.ishtype != 1 ? info.dmadedate : ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate));
                        trace12.dexpdate = (iSamePHProcess > 1 && CurrentSHMainInfo.ishtype != 1 ? info.dexpdate : ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate));
                        trace12.cmjph = "";
                        trace12.cirter = GlobalProxySetting.GetLoginState().EmployeeCode;
                        trace12.ctracename = ChooseGoodsInfo[0].ctracecode;
                        trace12.cparenttrace = ChooseGoodsInfo[0].cudicode;
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
                        trace12.corgid = CurrentSHMainInfo.corgid;
                        trace12.cnote = info.ref_cbilid;
                        CurrentTempEditTrace12 = trace12;
                        if (CurrentEditTrace12List != null && CurrentEditTrace12List.Any(p => p.cgoodsid == ChooseGoodsInfo[0].cgoodsid && p.cph == trace12.cph && p.ctracename == ChooseGoodsInfo[0].ctracecode))
                        {
                            if (info.PHList.Count == 0)
                            {
                                //去掉批号为空的数据，否则已收会多显示一条数据。
                                CurrentEditTrace12List.Remove(trace12);
                            }
                            if (CurrentEditTrace12List != null && CurrentEditTrace12List.Count > 0)
                            {
                                for (int i = 0; i < CurrentEditTrace12List.Count(); i++)
                                {
                                    if (CurrentEditTrace12List[i].cgoodsid == ChooseGoodsInfo[0].cgoodsid && CurrentEditTrace12List[i].cph == trace12.cph && CurrentEditTrace12List[i].ctracename == ChooseGoodsInfo[0].ctracecode)
                                    {
                                        CurrentEditTrace12List[i] = trace12;
                                        CurrentTempEditTrace12 = trace12;
                                        break;
                                    }
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
                //}
                //else
                //{

                //}
            }
        }
        /// <summary>
        /// 新流程保存UDI码
        /// </summary>
        /// <param name="info"></param>
        private void AddCurrentEditTraceListNewFlow(SHGoods info)
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
                            trace12.cph = (iSamePHProcess > 1 && CurrentSHMainInfo.ishtype != 1 ? info.cph : ChooseGoodsInfo[0].cph);
                            trace12.dmadedate = (iSamePHProcess > 1 && CurrentSHMainInfo.ishtype != 1 ? info.dmadedate : ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate));
                            trace12.dexpdate = (iSamePHProcess > 1 && CurrentSHMainInfo.ishtype != 1 ? info.dexpdate : ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate));
                            trace12.cmjph = "";
                            trace12.ctracename = child;
                            trace12.cdicode = ChooseGoodsInfo[0].ctracecode;
                            trace12.cirter = GlobalProxySetting.GetLoginState().EmployeeCode;
                            trace12.cparenttrace = ChooseGoodsInfo[0].cudicode;
                            trace12.cdicode = ChooseGoodsInfo[0].ctracecode;
                            trace12.fzsmqty = ChooseGoodsInfo[0].fqty;
                            trace12.corgid = CurrentSHMainInfo.corgid;
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
                            trace12.cph = (iSamePHProcess > 1 && CurrentSHMainInfo.ishtype != 1 ? info.cph : ChooseGoodsInfo[0].cph);
                            trace12.dmadedate = (iSamePHProcess > 1 && CurrentSHMainInfo.ishtype != 1 ? info.dmadedate : ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate));
                            trace12.dexpdate = (iSamePHProcess > 1 && CurrentSHMainInfo.ishtype != 1 ? info.dexpdate : ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate));
                            trace12.cmjph = "";
                            trace12.cirter = GlobalProxySetting.GetLoginState().EmployeeCode;
                            trace12.cparenttrace = ChooseGoodsInfo[0].cudicode;
                            trace12.ctracename = "";
                            trace12.cdicode = ChooseGoodsInfo[0].ctracecode;
                            //var findrow = CurrentEditTrace12List.Where(a => a.id1 == info.id1 && a.ctracename == "" && a.cparenttrace == ChooseGoodsInfo[0].cudicode).FirstOrDefault();
                            trace12.fzsmqty = ChooseGoodsInfo[0].fqty;

                            trace12.corgid = CurrentSHMainInfo.corgid;
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

        #endregion

        #region 校验数量

        /// <summary>
        /// 获取待收数量
        /// </summary>
        /// <returns></returns>
        private decimal CheckYshqty()
        {
            decimal reffqty = 0;

            if (CurrentEditGoods != null)
            {
                //还原本单的通知数量
                //if(CurrentEditGoodsList.Any(a => a.id1 == CurrentEditGoods.id1 && a.ref_cbilid != a.cbilid))
                //{
                //    var ref_fqty = CurrentEditGoodsList.FirstOrDefault(a => a.id1 == CurrentEditGoods.id1 && a.ref_cbilid != a.cbilid).ref_fqty;
                //    CurrentEditGoods.ref_fqty = ref_fqty;
                //}
                //else
                //{
                //    CurrentEditGoods.ref_fqty = CurrentEditGoods.ref_fqty + CurrentEditGoodsList.Sum(a => a.fqty);
                //}

                reffqty = CurrentEditGoods.ref_fqty - CurrentEditGoods.PHList.Sum(p => p.fshqty + p.fqsqty);

                if (CurrentEditGoods.ishtype == 1)//采购订单
                {
                    var ysshlist = CurrentEditGoodsList.Where(a => a.cgoodsid == CurrentEditGoods.cgoodsid && a.ref_cbilid == CurrentEditGoods.ref_cbilid && a.cph != CurrentEditGoods.cph).ToList();
                    if (ysshlist != null && ysshlist.Count > 0)//KB023 2023-06-19 校验数量修改，避免出现每次批号不一样，数量校验不正确问题
                    {
                        decimal ysfqyt = 0;
                        for (int i = 0; i < ysshlist.Count; i++)
                        {
                            //reffqty += ysshlist[i].ref_fqty;//需要补充另外一条明细的通知数量
                            if (ysshlist[i].PHList.Count > 0)
                            {
                                ysfqyt += ysshlist[i].PHList.Sum(p => p.fshqty + p.fqsqty);
                            }
                        }
                        reffqty = reffqty - ysfqyt;
                    }
                }
            }
            return reffqty;
        }

        //计算扫描数量
        private decimal CoumTracefqty(SHGoods info, string flag, ref bool bResult)
        {
            decimal dtmpfqty = 0;
            decimal dtmpfqty2 = 0;
            bResult = false;
            bool Notctracename = false;
            if (bchkUDI && ChooseGoodsInfo != null && ChooseGoodsInfo.Count > 0)
            {
                if (CurrentEditTrace12List == null) CurrentEditTrace12List = new List<GoodsTrace12>();
                if (CurrentEditTrace13List == null) CurrentEditTrace13List = new List<GoodsTrace13>();
                if (IsUseNewScanFlow)
                {
                    dtmpfqty2 = ChooseGoodsInfo[0].fqty * scanCount;
                }
                else
                {
                    dtmpfqty2 = ChooseGoodsInfo[0].fqty;
                }

                if (ChooseGoodsInfo[0].itype == 1)////UDI码处理
                {
                    //已经存在，则不需要修改数量//UDI存在时判断是否有相同序列号
                    if (ChooseGoodsInfo[0].childrentracecode != null && ChooseGoodsInfo[0].childrentracecode.Count > 0)
                    {
                        //if CurrentEditTrace12List.Where(a=>a.ctracename == ChooseGoodsInfo[0].childrentracecode.ForEach(y=>);
                        foreach (var child in ChooseGoodsInfo[0].childrentracecode)
                        {
                            if (CurrentEditTrace12List != null && CurrentEditTrace12List.Count > 0 && CurrentEditTrace12List.Where(a => a.ctracename == child).Any())
                            {
                                // bResult = true;
                                //校验是否重复扫码，重复扫码的不允许继续
                                //if (ds.Tables[d13Name].Select(string.Format("id1='{0}' and ctracename='{1}'", focusedRow["id1"], info.cudicode), "", DataViewRowState.CurrentRows).Any())
                                //    continue;
                                if (CurrentEditTrace13List != null && CurrentEditTrace13List.Count > 0 && CurrentEditTrace13List.Where(a => a.ctracename == ChooseGoodsInfo[0].cudicode).Any())
                                {
                                    bResult = true;
                                }
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
                        //dtmpfqty = CurrentEditTrace12List.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph == ChooseGoodsInfo[0].cph && a.id1 == info.id1).Sum(p => p.fzsmqty);
                        dtmpfqty = ConvertHelper.ToDecimal(this.txtSHSL.Text);
                    }
                    else if (Notctracename)
                    {
                        if (flag == "check")
                        {
                            if (info.fqty >= ConvertHelper.ToDecimal(this.txtSHSL.Text) + dtmpfqty2)//小于通知数量不允许新增
                            {
                                dtmpfqty = ConvertHelper.ToDecimal(this.txtSHSL.Text) + dtmpfqty2;
                            }
                            else
                            {
                                Toast.MakeText(this, "组合条码扫描数量[" + (ConvertHelper.ToDecimal(this.txtSHSL.Text) + dtmpfqty2).ToString() + "]不能大于收货数量[" + info.fqty.ToString() + "]，不允许此操作！", 0).Show();
                                bResult = true;
                                return ConvertHelper.ToDecimal(this.txtSHSL.Text);
                            }
                        }
                        else
                        {
                            bResult = true;
                            if (info.fqty < ConvertHelper.ToDecimal(this.txtSHSL.Text))
                            {
                                dtmpfqty = info.fqty;
                            }
                            else
                            {
                                dtmpfqty = ConvertHelper.ToDecimal(this.txtSHSL.Text);
                            }
                            return dtmpfqty;
                        }
                    }
                    else
                    {
                        //dtmpfqty = CurrentEditTrace12List.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph == ChooseGoodsInfo[0].cph && a.id1 == info.id1).Sum(p => p.fzsmqty) + dtmpfqty2;
                        dtmpfqty = ConvertHelper.ToDecimal(this.txtSHSL.Text) + dtmpfqty2;
                    }

                }
                else
                {
                    if (CurrentEditTrace12List != null && iSamePHProcess > 1 && CurrentEditTrace12List.Any(p => p.cgoodsid == ChooseGoodsInfo[0].cgoodsid && p.cph == ChooseGoodsInfo[0].cph && p.id1 == info.id1 && p.ctracename == ChooseGoodsInfo[0].ctracecode && p.dmadedate == ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate) && p.dexpdate == ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate)))
                    {
                        bResult = true;
                        //已经存在，则不需要修改数量
                    }
                    else if (CurrentEditTrace12List != null && iSamePHProcess <= 1 && CurrentEditTrace12List.Any(p => p.cgoodsid == ChooseGoodsInfo[0].cgoodsid && p.cph == ChooseGoodsInfo[0].cph && p.id1 == info.id1 && p.ctracename == ChooseGoodsInfo[0].ctracecode))
                    {
                        bResult = true;
                        //已经存在，则不需要修改数量
                    }
                    if (bResult)
                    {
                        if (iSamePHProcess > 1)
                        {
                            dtmpfqty = CurrentEditTrace12List.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph.Contains(ChooseGoodsInfo[0].cph) && a.id1 == info.id1 && a.dmadedate == ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate) && a.dexpdate == ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate)).Sum(p => p.fzsmqty);
                        }
                        else
                        {
                            dtmpfqty = CurrentEditTrace12List.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph == ChooseGoodsInfo[0].cph && a.id1 == info.id1).Sum(p => p.fzsmqty);
                        }
                    }
                    else
                    {
                        if (iSamePHProcess > 1)
                        {
                            dtmpfqty = CurrentEditTrace12List.Where(a => a.cgoodsid == ChooseGoodsInfo[0].cgoodsid && a.cph.Contains(ChooseGoodsInfo[0].cph) && a.id1 == info.id1 && a.dmadedate == ConvertHelper.ToString(ChooseGoodsInfo[0].dmadedate) && a.dexpdate == ConvertHelper.ToString(ChooseGoodsInfo[0].dexpdate)).Sum(p => p.fzsmqty) + dtmpfqty2;
                        }
                        else
                        {
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

        #endregion
 
    }

    #region Adapter

    class SHLRGoodsInfoAdapter : BaseAdapter<SHPHInfo>
    {
        LayoutInflater inflater;
        List<SHPHInfo> items;
        public SHLRGoodsInfoAdapter(Activity context, List<SHPHInfo> items)
        {
            this.inflater = LayoutInflater.From(context);
            this.items = items;
        }

        public override int Count
        {
            get
            {
                return this.items.Count;
            }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override SHPHInfo this[int position]
        {
            get
            {
                return this.items[position];
            }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = this.items[position];
            SHLRGoodsInfoViewHolder viewHolder = null;
            if (convertView == null)
            {
                viewHolder = new SHLRGoodsInfoViewHolder();
                convertView = inflater.Inflate(Resource.Layout.SHLRPHList, parent, false);
                convertView.DrawingCacheEnabled = true;
                convertView.DrawingCacheQuality = DrawingCacheQuality.High;
                viewHolder.t_ph = convertView.FindViewById<TextView>(Resource.Id.txt_ph);
                viewHolder.t_tzsl = convertView.FindViewById<TextView>(Resource.Id.txt_pdsl);
                viewHolder.t_shsl = convertView.FindViewById<TextView>(Resource.Id.txt_kcsl);
                viewHolder.t_qssl = convertView.FindViewById<TextView>(Resource.Id.txt_cysl);
                //viewHolder.t_ph.SetTextColor(Color.White);
                //viewHolder.t_tzsl.SetTextColor(Color.White);
                //viewHolder.t_shsl.SetTextColor(Color.White);
                //viewHolder.t_qssl.SetTextColor(Color.White);
                convertView.Tag = viewHolder;
            }
            else
            {
                viewHolder = convertView.Tag as SHLRGoodsInfoViewHolder;
            }
            viewHolder.Data = item;
            viewHolder.t_ph.Text = item.cph;
            viewHolder.t_tzsl.Text = item.fqty.ToString(GlobalDataCache.QtyFormat);
            viewHolder.t_shsl.Text = item.fshqty.ToString(GlobalDataCache.QtyFormat);
            viewHolder.t_qssl.Text = item.fqsqty.ToString(GlobalDataCache.QtyFormat);

            //viewHolder.t_tzsl.Visibility = GlobalDataCache.SHOWSTOCKQTY ? ViewStates.Visible : ViewStates.Invisible;
            //viewHolder.t_shsl.Visibility = GlobalDataCache.SHOWSTOCKQTY ? ViewStates.Visible : ViewStates.Invisible;
            //viewHolder.t_qssl.Visibility = GlobalDataCache.SHOWSTOCKQTY ? ViewStates.Visible : ViewStates.Invisible;
            return convertView;
        }

    }

    class SHLRGoodsInfoViewHolder : Java.Lang.Object
    {
        public TextView t_ph;
        public TextView t_tzsl;
        public TextView t_shsl;
        public TextView t_qssl;
        public SHPHInfo Data { get; set; }

    }

    public class ListViewPopWindowAdapter : PopupWindow
    {
        LayoutInflater inflater;
        Activity context;
        private List<SHGoods> items;
        public ListViewPopWindowAdapter(Activity context, List<SHGoods> items, AdapterView.IOnItemClickListener itemClickListener)
        {
            this.inflater = LayoutInflater.From(context);
            this.items = items;
            this.context = context;
            InitListView(itemClickListener);
        }
        private void InitListView(AdapterView.IOnItemClickListener itemClickListener)
        {
            View view = inflater.Inflate(Resource.Layout.ListViewWindow2, null);
            this.ContentView = view;
            //LayoutParams
            var parentView = (ViewGroup)view;
            var child = parentView.GetChildAt(0);
            this.Width = LayoutParams.WrapContent;
            this.Height = LayoutParams.WrapContent;
            this.Focusable = true;
            //ColorDrawable cdw = new ColorDrawable(Android.Graphics.Color.Transparent);
            //SetBackgroundDrawable(cdw);
            //View childView = ContentView

            var listView = view.FindViewById<ListView>(Resource.Id.listview);

            listView.Adapter = new SHLRGoodsInfoListViewAdapter(context, items);
            listView.OnItemClickListener = itemClickListener;
        }
    }
    public class SHLRGoodsInfoListViewAdapter : BaseAdapter<SHGoods>
    {
        LayoutInflater inflater;
        private List<SHGoods> items;
        public SHLRGoodsInfoListViewAdapter(Activity context, List<SHGoods> items)
        {
            this.inflater = LayoutInflater.From(context);
            this.items = items;
        }
        public override int Count
        {
            get
            {
                return this.items.Count;
            }
        }
        public override Java.Lang.Object GetItem(int position)
        {
            return null;
        }
        public override long GetItemId(int position)
        {
            return position;
        }
        public override SHGoods this[int position]
        {
            get
            {
                return this.items[position];
            }
        }
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = this.items[position];
            ViewHolder holder = null;
            if (convertView == null)
            {
                holder = new ViewHolder();
                convertView = inflater.Inflate(Resource.Layout.shlrlistview, parent, false);
                convertView.DrawingCacheEnabled = true;
                convertView.DrawingCacheQuality = DrawingCacheQuality.High;
                holder.t_cgoodsid = convertView.FindViewById<TextView>(Resource.Id.textView1);
                holder.t_frecqty = convertView.FindViewById<TextView>(Resource.Id.textView4);
                holder.t_dbildate = convertView.FindViewById<TextView>(Resource.Id.textView2);
                holder.t_cbilid = convertView.FindViewById<TextView>(Resource.Id.textView3);
                //holder.t_cgoodsid.SetTextColor(Color.White);//04E4FF  Color.ParseColor("#04E4FF")
                //holder.t_frecqty.SetTextColor(Color.White);//04E4FF
                //holder.t_dbildate.SetTextColor(Color.White);//04E4FF
                //holder.t_cbilid.SetTextColor(Color.White);//04E4FF

                //holder.t_cbilid.SetBackgroundColor(Color.Black);
                //holder.t_cgoodsid.SetBackgroundResource(Resource.Drawable.back);
                //holder.t_frecqty.SetBackgroundResource(Resource.Drawable.back);
                //holder.t_dbildate.SetBackgroundResource(Resource.Drawable.back);
                //holder.t_cbilid.SetBackgroundResource(Resource.Drawable.back);
                convertView.Tag = holder;
            }
            else
            {
                holder = (ViewHolder)convertView.Tag;
            }
            holder.Data = item;
            holder.t_cgoodsid.Text = item.cgoodsname + "(" + item.cgoodsid + ")";
            holder.t_dbildate.Text = item.dbildate;
            holder.t_cbilid.Text = item.ref_cbilid;
            holder.t_frecqty.Text = item.fqty.ToString(GlobalDataCache.QtyFormat);
            return convertView;
        }
        private class ViewHolder : Java.Lang.Object
        {
            public TextView t_cgoodsid;
            public TextView t_dbildate;
            public TextView t_cbilid;
            public TextView t_frecqty;
            public SHGoods Data { get; set; }
        }
    }

    #endregion
}