
/*********************************************************************************************************************/
//代码编写规则：
//1、定义引用程序集，格式必须为//reference:引用程序集文件名称(若需要引用外部程序集，请把外部程序集添加到程序运行目录下并且用{0}代替路径)
//2、必须继承BaseRequestProcesser
/*********************************************************************************************************************/

//reference:System.dll
//reference:System.Core.dll
//reference:System.Data.dll
//reference:System.Xml.dll
//reference:System.Xml.Linq.dll
//reference:{0}KingBos.Infrastructure.dll
//reference:{0}KingBos.DataAccess.dll
//reference:{0}KingBos.Business.dll
//reference:{0}ICSharpCode.SharpZipLib.dll

/* ------------------------------------------------
版本记录      版本日期      编辑人      编辑内容
V1.2          2025-08-11    CJJ       更新了新流程UDI码数据获取的
--------------------------------------------------- */

namespace KingBos.Business.Core
{
    #region 引用
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Text;
    using KingBos.Business.Context;
    using KingBos.Business.Core;
    using KingBos.Business.Utility;
    using KingBos.Business.Processer;
    using KingBos.DataAccess;
    using KingBos.Infrastructure;
    using KingBos.Infrastructure.Model;
    using KingBos.Infrastructure.Request;
    using KingBos.Infrastructure.Response;
    using KingBos.Infrastructure.Utility;
    //    using KingBos.Infrastructure.DynamicResponse;
    //    using KingBos.Infrastructure.DynamicRequest;
    //using ICSharpCode.SharpZipLib.Zip;
    //using ICSharpCode.SharpZipLib.Checksums;
    #endregion

    public class PDAXCSTOCKOPProcess : BaseRequestProcesser<PDAXCSTOCKOPRequest>
    {
        protected override BaseResponse Execute(PDAXCSTOCKOPRequest request)
        {
            //this.LoginInfo.EmpCode = request.EmpCode;
            var response = this.CreateResponse(request);
            if (request.OPType == 0)
            {
                #region 待出库复核单列表
                string sql = @"SELECT 
	a.dbildate,a.cbiltype,d.cname AS cbiltypename, cbilid,a.ref_cbilid,a.ref_cbiltype,
	b.cname as ref_cbiltypename,
	c.ccorpid AS ccorpid,c.ccorpname as ccorpname,
	a.ijhfhflag,
	a.corgid,
    r.corgname AS corgid_v_corgname,
	a.fhead_qty,
	a.fhead_value,
    a.iprintcount,
    e.cempname AS cname,
    a.ctagorgid,
    o.corgname AS ctagorgname
	FROM v_bl_outstock  a
	LEFT JOIN  dbo.sy_biltypecfg (nolock) b ON a.ref_cbiltype=b.cbiltype
	LEFT JOIN  dbo.v_bf_corp (nolock) c ON a.ccustid=c.ccorpid AND a.ishtype=c.ishtype
	LEFT JOIN  dbo.sy_biltypecfg (nolock) d ON a.cbiltype=d.cbiltype
	LEFT JOIN dbo.bf_employee (nolock) e ON a.chandler=e.cempid
	LEFT JOIN bf_org(NOLOCK) o ON a.ctagorgid=o.corgid
    LEFT JOIN bf_org(NOLOCK) r ON a.corgid=r.corgid
	WHERE a.iphtype=0 and a.cbilid NOT IN (SELECT ref_bilid FROM sy_holdbill) and a.iflag<100 and a.ijhfhflag=2 
              and a.corgid =@corgid";
                if (request.BeginDate != null && request.BeginDate != "")
                {
                    sql += " AND convert(varchar(10),a.dbildate,120) >= '" + request.BeginDate + "'";
                }
                if (request.EndDate != null && request.EndDate != "")
                {
                    sql += " AND convert(varchar(10),a.dbildate,120) < '" + Convert.ToDateTime(request.EndDate).AddDays(1).ToString("yyyy-MM-dd") + "'";
                }
                if (request.QueryText != null && request.QueryText != "")
                {
                    sql += " AND cbilid like  '%'+ @QueryText  +'%' ";
                }
                sql += " ORDER BY a.dbildate DESC, cbilid ";
                var dt = this.DBAccess.GetDataTable(sql, this.DBAccess.CreateDbParameter("@QueryText", request.QueryText)
                    , this.DBAccess.CreateDbParameter("@cempid", request.EmpCode), this.DBAccess.CreateDbParameter("@corgid", request.OrgID));
                response.Result = new List<XCSTOCKMainInfo>();
                foreach (var item in dt.Select())
                {
                    response.Result.Add(new XCSTOCKMainInfo()
                    {
                        cbilid = item["cbilid"].ToString(),
                        dbildate = item["dbildate"].ToString(),
                        cbiltype = item["cbiltype"].ToString(),
                        corgid = item["corgid"].ToString(),
                        corgname = item["corgid_v_corgname"].ToString(),
                        //cdeptid = item["cdeptid"].ToString(),
                        //cckid = item["cckid"].ToString(),
                        //cckname = item["cckid_v_cckname"].ToString(),
                        //chandler = item["chandler"].ToString(),
                        //cempname = item["chandler_v_cempname"].ToString(),
                        //csalerid = item["csalerid"].ToString(),
                        ctagorgid = item["ctagorgid"].ToString(),
                        ref_ctabname = item["ctagorgname"].ToString(),
                        crecer = "",// item["crecer"].ToString(),
                        //ientrustflag = item["ientrustflag"].ToInt(),
                        ref_cbilid = item["ref_cbilid"].ToString(),
                        ref_cbiltype = item["ref_cbiltype"].ToString(),
                        //ref_ctabname = item["ref_ctabname"].ToString(),
                        //irefrigerateflag = item["irefrigerateflag"].ToInt(),
                        fhead_qty = Convert.ToDecimal(item["fhead_qty"] == DBNull.Value ? 0 : item["fhead_qty"]),
                        fhead_value = Convert.ToDecimal(item["fhead_value"] == DBNull.Value ? 0 : item["fhead_value"]),
                        iflag = 0,//item["iflag"].ToInt(),
                        ijhfhflag = item["ijhfhflag"].ToInt(),
                        cnote = item["cbiltypename"].ToString(),
                        cpuaskcbilid = "",// item["cpuaskcbilid"].ToString(),
                        fpuaskfqty = 0,// item["fpuaskfqty"].ToInt(),
                        ref_corgid = "",// item["ref_corgid"].ToString(),
                        ref_corgid_address = "",// item["ref_corgid_address"].ToString(),
                        ccorpid = item["ccorpid"].ToString(),
                        ccorpname = item["ccorpname"].ToString(),
                        cempname = item["cname"].ToString()
                    });
                }
                #endregion
            }
            else if (request.OPType == 1)
            {
                //V1.2
                #region 待出库复核单据明细
                var sql = @"SELECT * FROM 
	(
	SELECT 
	a.id1,
	a.cbilid,
	a.ref_cbilid,
	a.ref_cbiltype,
	a.orig_cbiltype,
	a.orig_cbilid,
	a.orig_iid,
	a.orig_ctabname, 
	a.cgoodsid,
	b.ccommonname as cgoodsid_v_ccommonname,
	b.cgoodsname as cgoodsid_v_cgoodsname,
	c.cfactoryname as cgoodsid_v_cfactoryname,
	b.cpkname as cgoodsid_v_cpkname,
	b.iterm AS cgoodsid_v_iterm,
	b.cunit AS cgoodsid_v_cunit,
	b.cfileno AS cgoodsid_v_cfileno,
	b.cprodaddress AS cgoodsid_v_cprodaddress,
ISNULL(mah.cmahname,'')		AS cmahname,
ISNULL(mah.caddress,'')		AS caddress,
    b.czjmcode,
	b.cbarcode,
	b.cbarcode1,
	b.cbarcode2,
	b.cunit,
	b.cfileno,
	b.idayswarning,
	a.fjhqty AS fnoticeqty,
	a.fjhqty AS fqty,
	0 AS fcancelqty,
    a.ffhqty,
    NULL AS finputqty,
	'' AS cconclusion,
	b.cprodaddress,
	a.cph,
	a.dmadedate,
	a.dexpdate
	 ,ISNULL(b.fltp,1) AS cgoodsid_v_fltp
	 ,b.clunit AS  cgoodsid_v_clunit
	  ,b.cmunit AS cgoodsid_v_cmunit
	 ,ISNULL(b.fmtp,1) AS cgoodsid_v_fmtp
	  ,a.cmjph
	 ,a.dmjdate
	 ,a.dmjexpdate
	 ,a.cjzyl1  
	,a.cjzyl2   
	 ,CASE when b.isgspspecial=1 OR b.isgspprotein =1 THEN 1 ELSE 0 end AS ispegoodsflag,
	 b.ichinamedicine AS itraditionalgoodsflag
	 ,CASE WHEN b.isgspspecial = 1 AND b.isptype = 1 THEN 1 ELSE 0 END AS ihasspgoodslevelone
	 ,CASE WHEN b.isgspspecial = 1 AND b.isptype = 2 THEN 1 ELSE 0 END AS ihasspgoodsleveltwo
	 ,CASE WHEN b.isgspprotein = 1 THEN 1 ELSE 0 END AS hasspgoodsprotein
	 ,ISNULL(b.izsmflag,0) AS izsmflag
	 ,b.ichinamedicine AS izyflag
	 ,ISNULL(b.ifcflag,0) AS ifcflag
	,b.isgsphemp
    ,isgspspecial
    ,isptype
    ,b.ccertificateno
	FROM v_bl_outstock_d1 a
	LEFT JOIN bf_goods(NOLOCK) b ON a.cgoodsid=b.cgoodsid
              LEFT JOIN dbo.bf_mah(nolock) mah ON b.cmahid = mah.cmahid
	LEFT JOIN bf_factory(NOLOCK) c ON b.cfactoryid=c.cfactoryid
	WHERE a.ijhfhflag=2
) AS bl_xcstock_d1

 where bl_xcstock_d1.cbilid=@cbilid";
                var dt = this.DBAccess.GetDataTable(sql, this.DBAccess.CreateDbParameter("@cbilid", request.cbilid));
                response.GoodsList = new List<XCSTOCKGoods>();
                var IsEnableSFDARenewal = this.DBCache.Get("sy_config").Select("cparmname='IsEnableSFDARenewal' and cparmvalue='1'").Length > 0;//是否启用注册证换证功能
                foreach (var item in dt.Select())
                {
                    var info = new XCSTOCKGoods()
                    {
                        cgoodsid = item["cgoodsid"].ToString(),
                        cgoodsname = item["cgoodsid_v_cgoodsname"].ToString(),
                        ccommonname = item["cgoodsid_v_ccommonname"].ToString(),
                        czjmcode = item["czjmcode"].ToString(),
                        cbarcode = item["cbarcode"].ToString(),
                        cbarcode1 = item["cbarcode1"].ToString(),
                        cbarcode2 = item["cbarcode2"].ToString(),
                        cprodaddress = item["cgoodsid_v_cprodaddress"].ToString(),
                        cfileno = item["cgoodsid_v_cfileno"].ToString(),
                        cpkname = item["cgoodsid_v_cpkname"].ToString(),
                        cfactoryname = item["cgoodsid_v_cfactoryname"].ToString(),
                        cunit = item["cgoodsid_v_cunit"].ToString(),
                        iterm = item["cgoodsid_v_iterm"].ToInt(),
                        cph = item["cph"].ToString(),
                        dmadedate = item["dmadedate"].ToString(),
                        dexpdate = item["dexpdate"].ToString(),
                        cbilid = item["cbilid"].ToString(),
                        id1 = item["id1"].ToInt(),
                        fqty = Convert.ToDecimal(item["fqty"] == DBNull.Value ? 0 : item["fqty"]),
                        fnoticeqty = Convert.ToDecimal(item["fnoticeqty"] == DBNull.Value ? 0 : item["fnoticeqty"]),
                        ffhqty = Convert.ToDecimal(item["ffhqty"] == DBNull.Value ? 0 : item["ffhqty"]),
                        finputqty = 0,
                        orig_ctabname = item["orig_ctabname"].ToString(),
                        orig_cbiltype = item["orig_cbiltype"].ToString(),
                        orig_cbilid = item["orig_cbilid"].ToString(),
                        orig_iid = item["orig_iid"].ToString(),
                        ref_cbilid = item["ref_cbilid"].ToString(),
                        ref_cbiltype = item["ref_cbiltype"].ToString(),
                        isgsphemp = ConvertHelper.ToInt(item["isgsphemp"]),
                        isgspspecial = ConvertHelper.ToInt(item["isgspspecial"]),
                        isptype = ConvertHelper.ToInt(item["isptype"]),
                        cfilenofilelist = new List<PDAGoodscfilenofilelistRequest>(),
                        ccertificateno = ConvertHelper.ToString(item["ccertificateno"])//V1.2
                    };
                    if (IsEnableSFDARenewal)
                    {
                        sql = @"SELECT
                                cgoodsid
                                     ,cfileno
                                     ,dfilenodate
                                FROM bf_goods_filelist
                                WHERE cgoodsid = @cgoodsid ";
                        var cfilenoDT = this.DBAccess.GetDataTable(sql, this.DBAccess.CreateDbParameter("@cgoodsid", info.cgoodsid));
                        foreach (DataRow citem in cfilenoDT.Rows)
                        {
                            var cfile = new PDAGoodscfilenofilelistRequest()
                            {
                                cgoodsid = citem["cgoodsid"].ToString(),
                                cfileno = citem["cfileno"].ToString(),
                                dfilenodate = ConvertHelper.ToString(citem["dfilenodate"])
                            };
                            info.cfilenofilelist.Add(cfile);
                        }
                    }
                    response.GoodsList.Add(info);
                }
                #endregion
                #region 组合条码
                if (this.DBCache.Get("sy_config").Select("cparmname='INSTRCODE' and cparmvalue='1'").Length > 0)//启用了UDI参数
                {
                    if (this.DBCache.Get("sy_dt_tablelist").Select("tablename='bl_xcstock_d12'").Length > 0)
                    {
                        response.LRInfoTrace12List = new List<GoodsTrace12>();

                        //V1.2
                        if (ConvertHelper.ToInt(this.DBAccess.ExecuteScalar("SELECT TOP 1 cparmvalue from sy_config where cparmname='UseZsmNewProcess'")) == 1)
                        {
                            sql = @"
IF OBJECT_ID('tempdb..#temp_trace_d12_data') IS NOT NULL
	DROP TABLE #temp_trace_d12_data

SELECT tb.*
,bf_goods.cgoodsname as cgoodsid_v_cgoodsname,bf_goods.ccommonname as cgoodsid_v_ccommonname,bf_goods.cpkname as cgoodsid_v_cpkname,bf_goods.cprodaddress as cgoodsid_v_cprodaddress,bf_goods.cfactoryname as cgoodsid_v_cfactoryname,bf_goods.cfileno as cgoodsid_v_cfileno
,bf_goods.cunit as cgoodsid_v_cunit,bf_goods.iphflag as cgoodsid_v_iphflag,bf_goods.imjphflag as cgoodsid_v_imjphflag,bf_goods.izsmflag as cgoodsid_v_izsmflag,bf_goods.cbarcode as cgoodsid_v_cbarcode, bf_goods.cbarcode1 as cgoodsid_v_cbarcode1, bf_goods.cbarcode2 as cgoodsid_v_cbarcode2
, bf_goods.isgspcold as cgoodsid_v_isgspcold, bf_goods.iterm as cgoodsid_v_iterm, bf_goods.czjmcode as cgoodsid_v_czjmcode INTO #temp_trace_d12_data
FROM fun_getctracenamebycbilid(@cbilid) as tb 
INNER JOIN dbo.bf_goods(NOLOCK) on bf_goods.cgoodsid = tb.cgoodsid
--WHERE tb.cbilid = @cbilid 
--AND (tb.cgoodsid = @cgoodsid or isnull(@cgoodsid,'')='') AND (tb.id1 = @id1 or isnull(@id1,0)=0)

--IF NOT EXISTS (SELECT TOP 1 1 FROM #temp_trace_d12_data)
--BEGIN
--	INSERT INTO #temp_trace_d12_data(cbilid)
--	VALUES ('DELETE')
--END

SELECT * FROM #temp_trace_d12_data";
                        }
                        else
                        {
                            sql = "select * from bl_xcstock_d12 where cbilid=@cbilid";
                        }

                        dt = this.DBAccess.GetDataTable(sql, this.DBAccess.CreateDbParameter("@cbilid", request.cbilid));
                        foreach (var item12 in dt.Select())
                        {
                            //var findRow12 = response.LRInfoTrace12List.Where(p => p.cgoodsid == item12["cgoodsid"].ToString() && p.id1 == ConvertHelper.ToInt(item12["id1"])
                            //                    && p.ctracename == item12["ctracename"].ToString() && p.cph == item12["cph"].ToString()).FirstOrDefault();
                            //if (findRow12 == null)
                            //{
                            var GoodsTrace12 = new GoodsTrace12()
                            {
                                cbilid = ConvertHelper.ToString(item12["cbilid"]),
                                id1 = ConvertHelper.ToInt(item12["id1"]),
                                id12 = ConvertHelper.ToInt(item12["id12"]),
                                cgoodsid = ConvertHelper.ToString(item12["cgoodsid"]),
                                cph = ConvertHelper.ToString(item12["cph"]),
                                dmadedate = ConvertHelper.ToString(item12["dmadedate"]),
                                dexpdate = ConvertHelper.ToString(item12["dexpdate"]),
                                cmjph = ConvertHelper.ToString(item12["cmjph"]),
                                dmjdate = ConvertHelper.ToString(item12["dmjdate"]),
                                dmjexpdate = ConvertHelper.ToString(item12["dmjexpdate"]),
                                cparenttrace = ConvertHelper.ToString(item12["cparenttrace"]),
                                ctracename = ConvertHelper.ToString(item12["ctracename"]),
                                cdicode = ConvertHelper.ToString(item12["cdicode"]),
                                corgid = ConvertHelper.ToString(item12["corgid"]),
                                cirter = ConvertHelper.ToString(item12["cirter"]),
                                fzsmqty = ConvertHelper.ToDecimal(item12["fzsmqty"])
                            };
                            response.LRInfoTrace12List.Add(GoodsTrace12);
                            //}
                        }
                    }
                    if (this.DBCache.Get("sy_dt_tablelist").Select("tablename='bl_xcstock_d13'").Length > 0)
                    {
                        response.LRInfoTrace13List = new List<GoodsTrace13>();
                        sql = "select * from bl_xcstock_d13 where cbilid=@cbilid";
                        dt = this.DBAccess.GetDataTable(sql, this.DBAccess.CreateDbParameter("@cbilid", request.cbilid));
                        foreach (var item13 in dt.Select())
                        {
                            var findRow12 = response.LRInfoTrace12List.Where(p => p.cgoodsid == item13["cgoodsid"].ToString() && p.id1 == ConvertHelper.ToInt(item13["id1"])
                                                && p.ctracename == item13["ctracename"].ToString() && p.cph == item13["cph"].ToString()).FirstOrDefault();
                            if (findRow12 == null)
                            {
                                var GoodsTrace13 = new GoodsTrace13()
                                {
                                    cbilid = ConvertHelper.ToString(item13["cbilid"]),
                                    id1 = ConvertHelper.ToInt(item13["id1"]),
                                    id13 = ConvertHelper.ToInt(item13["id13"]),
                                    cgoodsid = ConvertHelper.ToString(item13["cgoodsid"]),
                                    cph = ConvertHelper.ToString(item13["cph"]),
                                    dmadedate = ConvertHelper.ToString(item13["dmadedate"]),
                                    dexpdate = ConvertHelper.ToString(item13["dexpdate"]),
                                    cmjph = ConvertHelper.ToString(item13["cmjph"]),
                                    dmjdate = ConvertHelper.ToString(item13["dmjdate"]),
                                    dmjexpdate = ConvertHelper.ToString(item13["dmjexpdate"]),
                                    ctracename = ConvertHelper.ToString(item13["ctracename"]),
                                    cirter = ConvertHelper.ToString(item13["cirter"]),
                                    falotqty = ConvertHelper.ToDecimal(item13["falotqty"]),
                                    cnote = ConvertHelper.ToString(item13["cnote"])
                                };
                                response.LRInfoTrace13List.Add(GoodsTrace13);
                            }
                        }
                    }

                }
                #endregion
            }
            else if (request.OPType == 10)//出库复核
            {
                #region 出库复核
                if (request.LRInfoList == null)
                {
                    response.IsError = true;
                    response.ErrorMessage = "参数不完整！";
                    return response;
                }
                if (request.cbilid == null || request.cbilid == "")
                {
                    response.IsError = true;
                    response.ErrorMessage = "单据编号不能为空！";
                    return response;
                }
                string cbilid = request.cbilid;
                string sTablename = "bl_xcstock";
                string sBilType = "XC";
                string sTablecname = "销售出库单";
                if (ConvertHelper.ToString(request.cbiltype) != "")
                {
                    sBilType = request.cbiltype;
                    var dtbiltype = this.DBAccess.GetDataTable(string.Format("SELECT TOP 1 * FROM sy_biltypecfg WHERE cbiltype='{0}'", request.cbiltype));
                    if (dtbiltype != null && dtbiltype.Rows.Count > 0)
                    {
                        if (ConvertHelper.ToString(dtbiltype.Rows[0]["ctabname"]) != "")
                        {
                            sTablename = ConvertHelper.ToString(dtbiltype.Rows[0]["ctabname"]);
                        }
                        if (ConvertHelper.ToString(dtbiltype.Rows[0]["cname"]) != "")
                        {
                            sTablecname = ConvertHelper.ToString(dtbiltype.Rows[0]["cname"]);
                        }
                    }
                }

                var iflag = this.DBAccess.ExecuteScalar(string.Format("SELECT iflag FROM {0}_d1 (nolock) where cbilid=@cbilid", sTablename), this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                if (ConvertHelper.ToString(iflag) == "100")
                {
                    response.IsError = true;
                    response.ErrorMessage = "此" + sTablecname + "[" + cbilid + "]已过账！";
                    return response;
                }
                //PDA未入库的，需要删除明细
                string id1s = "";
                var findRow2 = this.DBCache.Get("sy_biltypecfg").Select("cbiltype='" + sBilType + "' and ISNULL(csaveprocname,'')<>''").FirstOrDefault();
                string sumfield = "";
                string prconame = "";
                if (findRow2 != null)
                {
                    sumfield = findRow2["csumfield"].ToString();
                    prconame = findRow2["csaveprocname"].ToString();
                }
                Transaction tran = null;
                //tran = this.DBAccess.CreateTransaction();
                var dt = this.DBAccess.GetDataTable(string.Format("SELECT * FROM {0}_d1 (nolock) where cbilid=@cbilid", sTablename), this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                dt.TableName = sTablename + "_d1";
                /*   2024-03-07 注释该代码
                foreach (var item in request.LRInfoList.Where(p => p.iflag == 0))
                {
                    if (findRow2 != null)
                    {
                        if (item.ref_cbilid != "" && item.ref_cbilid != "-")
                        {
                            //重新计算转单数量
                            var paraArray = new List<DbParameter>() { };
                            paraArray.Add(this.DBAccess.CreateDbParameter("@in_cbiltype", sBilType));
                            paraArray.Add(this.DBAccess.CreateDbParameter("@in_cbilid", cbilid));
                            paraArray.Add(this.DBAccess.CreateDbParameter("@in_iid", 0));
                            paraArray.Add(this.DBAccess.CreateDbParameter("@in_reftabname", item.ref_ctabname));
                            paraArray.Add(this.DBAccess.CreateDbParameter("@in_refcbilid", item.ref_cbilid));
                            paraArray.Add(this.DBAccess.CreateDbParameter("@in_refiid", item.ref_iid));
                            paraArray.Add(this.DBAccess.CreateDbParameter("@in_fqty", 0));
                            int a = this.DBAccess.ExecuteStoredProcedure(prconame, paraArray.ToArray());
                        }
                    }

                }
                 */
                var IsEnableSFDARenewal = this.DBCache.Get("sy_config").Select("cparmname='IsEnableSFDARenewal' and cparmvalue='1'").Length > 0;//是否启用注册证换证功能
                foreach (var item in request.LRInfoList.Where(p => (p.ffhqty > 0 || p.finputqty > 0)))
                {
                    //PDA复核数据
                    var findRow = dt.Select(string.Format("cgoodsid='{0}' and cbilid='{1}' and id1='{2}' "
                                              , item.cgoodsid, item.cbilid, item.id1)).FirstOrDefault();
                    if (findRow != null)
                    {
                        findRow["cph"] = item.cph;
                        if (item.dmadedate != "")
                        {
                            findRow["dmadedate"] = ConvertHelper.ToDateTime(item.dmadedate);
                        }
                        if (item.dexpdate != "")
                        {
                            findRow["dexpdate"] = ConvertHelper.ToDateTime(item.dexpdate);
                        }
                        if (IsEnableSFDARenewal)
                        {
                            findRow["cphnote2"] = item.cphnote2;
                        }
                        findRow["fqty"] = item.ffhqty;
                        findRow["ffhqty"] = item.ffhqty;
                        findRow["fcancelqty"] = item.finputqty;
                        findRow["ccancelseason"] = item.ccancelseason;
                        findRow["cfher1"] = request.EmpCode;
                        findRow["cfher2"] = (string.IsNullOrEmpty(request.cfher2) ? "" : request.cfher2);
                        findRow["dfhdatetime"] = DateTime.Now;
                        //findRow["ijhfhflag"] = 100;
                        if (findRow.Table.Columns.Contains("ctracecode"))//2024-07-25
                        {
                            if (request.LRInfoTrace12List != null && request.LRInfoTrace12List.Count > 0)
                            {
                                var find12List = request.LRInfoTrace12List.Where(a => a.id1 == ConvertHelper.ToInt(item.id1)).ToList();
                                if (find12List != null && find12List.Count > 0)
                                {
                                    var ctracecodes = find12List.GroupBy(a => a.ctracename).Aggregate("", (c, r) => c + ";" + r.Key);
                                    if (!string.IsNullOrEmpty(ctracecodes) && ctracecodes.Length > 1)
                                    {
                                        findRow["ctracecode"] = ctracecodes.Substring(1, ctracecodes.Length - 1);
                                    }
                                }
                            }
                        }
                    }
                }
                //if (!this.DBAccess.Save(dt, tran))
                //{
                //    tran.RollBack();
                //    response.IsError = true;
                //    response.ErrorMessage = "保存" + sTablecname + "-数据子表[" + sBilType + "_d1]失败!";
                //    return response;

                //}
                #region 组合条码
                DataTable dt12 = null;
                DataTable dt13 = null;
                if (this.DBCache.Get("sy_config").Select("cparmname='INSTRCODE' and cparmvalue='1'").Length > 0)//KB023 UDI保存
                {
                    //V1.2
                    var isNewFlow = (ConvertHelper.ToInt(this.DBAccess.ExecuteScalar("SELECT TOP 1 cparmvalue from sy_config where cparmname='UseZsmNewProcess'")) == 1);
                    var d12FrontTablename = isNewFlow ? "bl_out" : sTablename;

                    #region 保存D12表
                    if (request.LRInfoTrace12List != null)// && request.LRInfoTrace12List.Count > 0 这个不能加，如果存在删除码操作这里就不会被正确执行
                    {
                        if (this.DBCache.Get("sy_dt_tablelist").Select("tablename='" + d12FrontTablename + "_d12'").Length > 0)
                        {
                            if (isNewFlow)
                            {
                                //V1.2 CJJ PDA采码改造
                                //先把本单追溯码的数据取下来放到缓存中,因为主页面中只有新的UDI采集数据（UDI码可重复采集，无需在后续校验）
                                dt12 = this.DBAccess.GetDataTable(string.Format("SELECT * FROM {0}_d12(nolock) where cbilid=@cbilid AND cdatatype = 'ZSM'", d12FrontTablename), this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                                dt12.TableName = d12FrontTablename + "_d12";

                                if (request.LRInfoTrace12List.Count <= 0) dt12.Clear();

                                this.DBAccess.ExecuteNonQuery(string.Format("delete from {0}_d12 where cbilid=@cbilid", d12FrontTablename), this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                            }
                            else
                            {
                                dt12 = this.DBAccess.GetDataTable(string.Format("SELECT * FROM {0}_d12(nolock) where cbilid=@cbilid", d12FrontTablename), this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                                dt12.TableName = d12FrontTablename + "_d12";

                                this.DBAccess.ExecuteNonQuery(string.Format("delete from {0}_d12 where cbilid=@cbilid", d12FrontTablename), this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                            }


                            var maxID12 = dt12.Compute("MAX(id12)", "");

                            if (maxID12 == null || maxID12 == DBNull.Value)
                            {
                                maxID12 = 0;
                            }
                            foreach (var item12 in request.LRInfoTrace12List)
                            {
                                maxID12 = Convert.ToInt32(maxID12) + 1;
                                DataRow newRow = null;
                                newRow = dt12.NewRow();

                                //V1.2
                                if (dt12 != null && dt12.Select().Length > 0 && dt12.Select(string.Format("ctracename='{0}' AND cdatatype = 'ZSM'", item12.ctracename)).Any())
                                {
                                    var originDataRow = dt12.Select(string.Format("ctracename='{0}'", item12.ctracename)).FirstOrDefault();
                                    if (originDataRow != null)
                                    {
                                        originDataRow.SetAdded();
                                        //DataRowHelper.SetRowValue(originDataRow, newRow);
                                    }
                                    //这里涉及到一个行状态的问题
                                    //SysLog.WriteLocalLog("旧数据重新添加到dt12,Begin", "PDA改造日志！！！");
                                    //dt12.Rows.Add(newRow);
                                    //SysLog.WriteLocalLog("旧数据重新添加到dt12,End", "PDA改造日志！！！");
                                    continue;
                                }


                                DataRowHelper.SetDefaultValue(newRow);
                                newRow["cbilid"] = cbilid;
                                newRow["cbiltype"] = sBilType;
                                newRow["id1"] = item12.id1;
                                newRow["id12"] = maxID12;
                                newRow["cgoodsid"] = item12.cgoodsid;
                                newRow["cph"] = item12.cph;

                                if (isNewFlow)
                                {
                                    newRow["cdatatype"] = "UDI";
                                    newRow["ireplenishflag"] = 0;
                                    newRow["fbaseqty"] = item12.fzsmqty;
                                    newRow["clastupdnote"] = "PDA采集";
                                }

                                if (!string.IsNullOrWhiteSpace(item12.dmadedate))
                                {
                                    newRow["dmadedate"] = Convert.ToDateTime(item12.dmadedate);
                                }
                                else
                                {
                                    newRow["dmadedate"] = DBNull.Value;
                                }
                                if (!string.IsNullOrWhiteSpace(item12.dexpdate))
                                {
                                    newRow["dexpdate"] = Convert.ToDateTime(item12.dexpdate);
                                }
                                else
                                {
                                    newRow["dexpdate"] = DBNull.Value;
                                }
                                newRow["cmjph"] = item12.cmjph;
                                if (!string.IsNullOrWhiteSpace(item12.dmjdate))
                                {
                                    newRow["dmjdate"] = Convert.ToDateTime(item12.dmjdate);
                                }
                                else
                                {
                                    newRow["dmjdate"] = DBNull.Value;
                                }
                                if (!string.IsNullOrWhiteSpace(item12.dmjexpdate))
                                {
                                    newRow["dmjexpdate"] = Convert.ToDateTime(item12.dmjexpdate);
                                }
                                else
                                {
                                    newRow["dmjexpdate"] = DBNull.Value;
                                }

                                newRow["cparenttrace"] = item12.cparenttrace;//父级追溯码，记录完整UDI码
                                newRow["ctracename"] = item12.ctracename;
                                if (newRow.Table.Columns.Contains("cdicode"))
                                {
                                    newRow["cdicode"] = item12.cdicode;//UDI码的 DI码
                                }
                                newRow["corgid"] = item12.corgid;
                                newRow["dscandate"] = DateTime.Now;
                                newRow["cirter"] = request.EmpCode;
                                newRow["fzsmqty"] = item12.fzsmqty;
                                dt12.Rows.Add(newRow);

                            }
                            //if(!this.DBAccess.Save(dt12, tran))
                            //{
                            //    tran.RollBack();
                            //    response.IsError = true;
                            //    response.ErrorMessage = "保存" + sTablecname + "-数据子表["+d12FrontTablename+"_d12]失败!";
                            //    return response;
                            //}
                        }
                    }
                    #endregion
                    #region 保存D13表
                    if (request.LRInfoTrace13List != null && request.LRInfoTrace13List.Count > 0)
                    {
                        if (this.DBCache.Get("sy_dt_tablelist").Select("tablename='bl_xcstock_d13'").Length > 0)
                        {
                            this.DBAccess.ExecuteNonQuery(string.Format("delete from {0}_d13 where cbilid=@cbilid", sTablename), this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                            dt13 = this.DBAccess.GetDataTable(string.Format("SELECT * FROM {0}_d13(nolock) where cbilid=@cbilid", sTablename), this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                            dt13.TableName = sTablename + "_d13";

                            var maxID13 = dt13.Compute("MAX(id13)", "");

                            if (maxID13 == null || maxID13 == DBNull.Value)
                            {
                                maxID13 = 0;
                            }
                            foreach (var item13 in request.LRInfoTrace13List)
                            {
                                maxID13 = Convert.ToInt32(maxID13) + 1;
                                DataRow newRow = null;
                                newRow = dt13.NewRow();
                                DataRowHelper.SetDefaultValue(newRow);
                                newRow["id1"] = item13.id1;
                                newRow["id13"] = maxID13;
                                newRow["cbilid"] = cbilid;
                                newRow["cgoodsid"] = item13.cgoodsid;
                                newRow["cph"] = item13.cph;
                                if (!string.IsNullOrWhiteSpace(item13.dmadedate))
                                {
                                    newRow["dmadedate"] = Convert.ToDateTime(item13.dmadedate);
                                }
                                else
                                {
                                    newRow["dmadedate"] = DBNull.Value;
                                }
                                if (!string.IsNullOrWhiteSpace(item13.dexpdate))
                                {
                                    newRow["dexpdate"] = Convert.ToDateTime(item13.dexpdate);
                                }
                                else
                                {
                                    newRow["dexpdate"] = DBNull.Value;
                                }
                                newRow["cmjph"] = item13.cmjph;
                                if (!string.IsNullOrWhiteSpace(item13.dmjdate))
                                {
                                    newRow["dmjdate"] = Convert.ToDateTime(item13.dmjdate);
                                }
                                else
                                {
                                    newRow["dmjdate"] = DBNull.Value;
                                }
                                if (!string.IsNullOrWhiteSpace(item13.dmjexpdate))
                                {
                                    newRow["dmjexpdate"] = Convert.ToDateTime(item13.dmjexpdate);
                                }
                                else
                                {
                                    newRow["dmjexpdate"] = DBNull.Value;
                                }
                                newRow["ctracename"] = item13.ctracename;
                                newRow["cirter"] = request.EmpCode;
                                newRow["dirtdate"] = DateTime.Now;
                                newRow["falotqty"] = item13.falotqty;
                                newRow["cnote"] = item13.cnote;
                                dt13.Rows.Add(newRow);

                            }
                            //if (!this.DBAccess.Save(dt13, tran))
                            //{
                            //    tran.RollBack();
                            //    response.IsError = true;
                            //    response.ErrorMessage = "保存" + sTablecname + "-数据子表[" + sTablename + "_d13]失败!";
                            //    return response;
                            //}
                        }
                    }
                    #endregion
                }
                #endregion
                //tran.Commit();

                //这里处理了追溯码的保存 会按参数ISOUTSTOCKAUTOACC判断是否自动过账
                var request2 = new BusinessRequest() { BusinessKey = "UpdateWMSPickreChkDetailGoodsProcess" };
                request2.Parameters["cfher1"] = request.EmpCode;
                request2.Parameters["cfher2"] = (string.IsNullOrEmpty(request.cfher2) ? "" : request.cfher2); ;
                request2.Parameters["cbilid"] = cbilid;
                var lstGoods = new List<string>();
                foreach (var row in dt.Select(""))
                {
                    lstGoods.Add(string.Format("{0};{1};{2}", row["id1"], row["fqty"] == DBNull.Value ? 0 : (decimal)row["fqty"], row.Table.Columns.Contains("ccancelseason") ? row["ccancelseason"].ToString() : ""));
                }
                request2.Parameters["goods"] = lstGoods;
                request2.Parameters["d12"] = dt12;
                //V1.2 
                if (ConvertHelper.ToInt(this.DBAccess.ExecuteScalar("SELECT TOP 1 cparmvalue from sy_config where cparmname='UseZsmNewProcess'")) != 1)
                {
                    request2.Parameters["d13"] = dt13;
                }

                request2.LoginState = request.LoginState;
                if (request2.LoginState != null) request2.LoginState.EmployeeCode = request.EmpCode;
                if (this.LoginInfo != null && this.LoginInfo.EmpCode == null) this.LoginInfo.EmpCode = request.EmpCode;

                var response2 = ExecuteOther(request2);
                if (response2.IsError)
                {
                    response.IsError = true;
                    response.ErrorMessage = response2.ErrorMessage;
                    return response;
                }
                var msg = response2.Result != null ? (response2.Result.ContainsKey("AccOutSotckMsg") ? response2.Result["AccOutSotckMsg"].ToString() : "") : "";
                if (msg.Contains("请检查追溯码采集数据"))
                {
                    response.ErrorMessage = "复核失败！" + msg;
                    return response;
                }
                if (!msg.Contains("注意："))
                {
                    if (sBilType == "XC" && this.DBCache.Get("sy_config").Select("cparmname='EnableBoxOutStock' and cparmvalue='1'").Length > 0)
                    {
                        tran = this.DBAccess.CreateTransaction();
                        //更新复核标识
                        var sql = string.Format("update {0} set ijhfhflag=100 where  ijhfhflag<>100 and cbilid=@cbilid  ", sTablename);
                        var execResult = this.DBAccess.ExecuteNonQuery(sql, tran, this.DBAccess.CreateDbParameter("@cbilid", request.cbilid));

                        if (sBilType == "XC" && this.DBCache.Get("sy_config").Select("cparmname='EnableBoxOutStock' and cparmvalue='1'").Length > 0)//装箱单
                        {

                            sql = string.Format(@"
EXEC sp_bl_fhd_to_bl_chzxd @in_coutbilid,@in_corgid,@in_cuserid,@in_fzjqty,@in_fsjqty

SELECT DISTINCT cbilid FROM dbo.bl_chzxd_d1 WHERE coutbilid = @in_coutbilid");

                            var dtzx = this.DBAccess.GetDataTable(sql, tran, this.DBAccess.CreateDbParameter("@in_coutbilid", request.cbilid)
                                                                           , this.DBAccess.CreateDbParameter("@in_corgid", request.OrgID)
                                                                           , this.DBAccess.CreateDbParameter("@in_cuserid", request.EmpCode)
                                                                           , this.DBAccess.CreateDbParameter("@in_fzjqty", 0)
                                                                           , this.DBAccess.CreateDbParameter("@in_fsjqty", 0)
                                                                           );


                            if (!(dtzx != null && dtzx.Rows.Count > 0))
                            {
                                tran.RollBack();
                                response.IsError = true;
                                response.ErrorMessage = "生成出货装箱单失败!";
                                return response;
                            }
                        }
                        else if (this.DBCache.Get("sy_config").Select("cparmname='ISOUTSTOCKAUTOACC' and cparmvalue='1'").Length > 0)//自动过账出库单
                        {
                            var result = this.DBAccess.ExecuteStoredProcedure("sp_bl_outstock", tran, this.DBAccess.CreateDbParameter("@in_cmoduleid", "-")
                                                                                                  , this.DBAccess.CreateDbParameter("@in_cbiltype", sBilType)
                                                                                                  , this.DBAccess.CreateDbParameter("@in_cbilid", request.cbilid)
                                                                                                  , this.DBAccess.CreateDbParameter("@in_cuserid", request.EmpCode)
                                                                                                  , this.DBAccess.CreateDbParameter("@in_cactiontype", "acc")
                                                                                                  , this.DBAccess.CreateDbParameter("@in_caction", "acc-ok")
                                                                                                  , this.DBAccess.CreateDbParameter("@in_cnote", "")
                                                                                                  , this.DBAccess.CreateDbParameter("@in_itranflag", 1)
                                                                                                  );

                            if (!(result == 0))
                            {
                                tran.RollBack();
                                response.IsError = true;
                                response.ErrorMessage = string.Format(sTablecname + "号【{0}】,自动过账失败！请手动过账！");
                                return response;
                            }
                        }
                        tran.Commit();
                        //this.DBAccess.ExecuteNonQuery("DELETE FROM bf_ckfh_data WHERE cbilid=@cbilid "
                        //            , this.DBAccess.CreateDbParameter("@cbilid", cbilid)
                        //);
                    }
                }
                if (!string.IsNullOrEmpty(msg))
                {
                    response.ErrorMessage = msg;
                }
                #endregion
            }
            else if (request.OPType == 11)//复核历史
            {

                #region 获取复核历史
                var sql = @"SELECT  a.dbildate,a.cbiltype,d.cname AS cbiltypename, cbilid,a.ref_cbilid,a.ref_cbiltype,b.cname as ref_cbiltypename,c.ccorpid AS ccorpid,c.ccorpname as ccorpname,
	a.ijhfhflag,a.corgid,a.fhead_qty,a.fhead_value,a.iprintcount,e.cempname AS cname,a.ctagorgid FROM v_bl_outstock  a
	LEFT JOIN  dbo.sy_biltypecfg (nolock) b ON a.ref_cbiltype=b.cbiltype
	LEFT JOIN  dbo.v_bf_corp (nolock) c ON a.ccustid=c.ccorpid AND a.ishtype=c.ishtype
	LEFT JOIN  dbo.sy_biltypecfg (nolock) d ON a.cbiltype=d.cbiltype
	LEFT JOIN dbo.bf_employee (nolock) e ON a.chandler=e.cempid
	WHERE a.ijhfhflag=100 
              and a.corgid =@corgid";

                if (request.BeginDate != null && request.BeginDate != "")
                {
                    sql += " AND convert(varchar(10),a.dbildate,120) >= '" + request.BeginDate + "'";
                }
                if (request.EndDate != null && request.EndDate != "")
                {
                    sql += " AND convert(varchar(10),a.dbildate,120) < '" + Convert.ToDateTime(request.EndDate).AddDays(1).ToString("yyyy-MM-dd") + "'";
                }
                if (request.QueryText != null && request.QueryText != "")
                {
                    sql += " AND cbilid like  '%'+ @QueryText  +'%' ";
                }
                sql += " ORDER BY a.dbildate DESC, cbilid ";
                var dt = this.DBAccess.GetDataTable(sql, this.DBAccess.CreateDbParameter("@QueryText", request.QueryText)
                    , this.DBAccess.CreateDbParameter("@cempid", request.EmpCode), this.DBAccess.CreateDbParameter("@corgid", request.OrgID));
                response.Result = new List<XCSTOCKMainInfo>();
                var IsEnableSFDARenewal = this.DBCache.Get("sy_config").Select("cparmname='IsEnableSFDARenewal' and cparmvalue='1'").Length > 0;//是否启用注册证换证功能
                foreach (var Hitem in dt.Select())
                {
                    var MainInfo = (new XCSTOCKMainInfo()
                    {
                        cbilid = Hitem["cbilid"].ToString(),
                        dbildate = Hitem["dbildate"].ToString(),
                        cbiltype = Hitem["cbiltype"].ToString(),
                        corgid = Hitem["corgid"].ToString(),
                        ctagorgid = Hitem["ctagorgid"].ToString(),
                        crecer = "",// item["crecer"].ToString(),
                        //ientrustflag = item["ientrustflag"].ToInt(),
                        ref_cbilid = Hitem["ref_cbilid"].ToString(),
                        ref_cbiltype = Hitem["ref_cbiltype"].ToString(),
                        //ref_ctabname = item["ref_ctabname"].ToString(),
                        //irefrigerateflag = item["irefrigerateflag"].ToInt(),
                        fhead_qty = Convert.ToDecimal(Hitem["fhead_qty"] == DBNull.Value ? 0 : Hitem["fhead_qty"]),
                        fhead_value = Convert.ToDecimal(Hitem["fhead_value"] == DBNull.Value ? 0 : Hitem["fhead_value"]),
                        iflag = 0,//item["iflag"].ToInt(),
                        ijhfhflag = Hitem["ijhfhflag"].ToInt(),
                        cnote = Hitem["cbiltypename"].ToString(),
                        cpuaskcbilid = "",// item["cpuaskcbilid"].ToString(),
                        fpuaskfqty = 0,// item["fpuaskfqty"].ToInt(),
                        ref_corgid = "",// item["ref_corgid"].ToString(),
                        ref_corgid_address = "",// item["ref_corgid_address"].ToString(),
                        ccorpid = Hitem["ccorpid"].ToString(),
                        ccorpname = Hitem["ccorpname"].ToString(),
                        xcstockgoodsList = new List<XCSTOCKGoods>()
                    });
                    //V1.2
                    //明细赋值
                    sql = @"SELECT * FROM 
	                        (SELECT a.dfhdatetime,a.id1,a.cbilid,a.ref_cbilid,a.ref_cbiltype,a.cgoodsid,b.ccommonname as cgoodsid_v_ccommonname,b.cgoodsname as cgoodsid_v_cgoodsname,c.cfactoryname as cgoodsid_v_cfactoryname,
	                        b.cpkname as cgoodsid_v_cpkname,b.iterm AS cgoodsid_v_iterm,b.cbarcode,b.cunit,b.cfileno,b.cprodaddress as cgoodsid_v_cprodaddress,a.fnoticeqty,a.fjhqty,a.ffhqty,a.fcancelqty,a.ccancelseason,
	                        a.cph,a.dmadedate,a.dexpdate,a.cmjph,a.dmjdate,a.dmjexpdate,b.ccertificateno FROM v_bl_outstock_d1 a LEFT JOIN v_bf_goods(NOLOCK) b ON a.cgoodsid=b.cgoodsid LEFT JOIN bf_factory(NOLOCK) c ON b.cfactoryid=c.cfactoryid
	                        WHERE a.ijhfhflag=100 and a.corgid=@corgid and a.cbilid=@cbilid
                        ) AS T";
                    dt = this.DBAccess.GetDataTable(sql, this.DBAccess.CreateDbParameter("@corgid", Hitem["corgid"].ToString()), this.DBAccess.CreateDbParameter("@cbilid", Hitem["cbilid"].ToString()));
                    //response.GoodsList = new List<XCSTOCKGoods>();

                    foreach (var item in dt.Select())
                    {
                        var info = new XCSTOCKGoods()
                        {
                            cgoodsid = item["cgoodsid"].ToString(),
                            cgoodsname = item["cgoodsid_v_cgoodsname"].ToString(),
                            ccommonname = item["cgoodsid_v_ccommonname"].ToString(),
                            cbarcode = item["cbarcode"].ToString(),
                            cprodaddress = item["cgoodsid_v_cprodaddress"].ToString(),
                            cpkname = item["cgoodsid_v_cpkname"].ToString(),
                            cfactoryname = item["cgoodsid_v_cfactoryname"].ToString(),
                            cunit = item["cunit"].ToString(),
                            iterm = item["cgoodsid_v_iterm"].ToInt(),
                            cph = item["cph"].ToString(),
                            dmadedate = item["dmadedate"].ToString(),
                            dexpdate = item["dexpdate"].ToString(),
                            cbilid = item["cbilid"].ToString(),
                            id1 = item["id1"].ToInt(),
                            ffhqty = Convert.ToDecimal(item["ffhqty"] == DBNull.Value ? 0 : item["ffhqty"]),
                            fjhqty = Convert.ToDecimal(item["fjhqty"] == DBNull.Value ? 0 : item["fjhqty"]),
                            fcancelqty = Convert.ToDecimal(item["fcancelqty"] == DBNull.Value ? 0 : item["fcancelqty"]),
                            fnoticeqty = Convert.ToDecimal(item["fnoticeqty"] == DBNull.Value ? 0 : item["fnoticeqty"]),
                            cfilenofilelist = new List<PDAGoodscfilenofilelistRequest>(),
                            ccertificateno = ConvertHelper.ToString(item["ccertificateno"])//V1.2
                        };
                        if (IsEnableSFDARenewal)
                        {
                            sql = @"SELECT
                                cgoodsid
                                     ,cfileno
                                     ,dfilenodate
                                FROM bf_goods_filelist
                                WHERE cgoodsid = @cgoodsid ";
                            var cfilenoDT = this.DBAccess.GetDataTable(sql, this.DBAccess.CreateDbParameter("@cgoodsid", info.cgoodsid));
                            foreach (DataRow citem in cfilenoDT.Rows)
                            {
                                var cfile = new PDAGoodscfilenofilelistRequest()
                                {
                                    cgoodsid = citem["cgoodsid"].ToString(),
                                    cfileno = citem["cfileno"].ToString(),
                                    dfilenodate = ConvertHelper.ToString(citem["dfilenodate"])
                                };
                                info.cfilenofilelist.Add(cfile);
                            }
                        }
                        MainInfo.xcstockgoodsList.Add(info);
                    }
                    response.Result.Add(MainInfo);
                }

                #endregion
            }
            //V1.2增加采码前保存响应
            else if (request.OPType == 12)
            {
                if (request.LRInfoList == null)
                {
                    response.IsError = true;
                    response.ErrorMessage = "参数不完整！";
                    return response;
                }
                if (request.cbilid == null || request.cbilid == "")
                {
                    response.IsError = true;
                    response.ErrorMessage = "单据编号不能为空！";
                    return response;
                }
                string cbilid = request.cbilid;
                string sTablename = "bl_xcstock";
                string sBilType = "XC";
                string sTablecname = "销售出库单";
                if (ConvertHelper.ToString(request.cbiltype) != "")
                {
                    sBilType = request.cbiltype;
                    var dtbiltype = this.DBAccess.GetDataTable(string.Format("SELECT TOP 1 * FROM sy_biltypecfg WHERE cbiltype='{0}'", request.cbiltype));
                    if (dtbiltype != null && dtbiltype.Rows.Count > 0)
                    {
                        if (ConvertHelper.ToString(dtbiltype.Rows[0]["ctabname"]) != "")
                        {
                            sTablename = ConvertHelper.ToString(dtbiltype.Rows[0]["ctabname"]);
                        }
                        if (ConvertHelper.ToString(dtbiltype.Rows[0]["cname"]) != "")
                        {
                            sTablecname = ConvertHelper.ToString(dtbiltype.Rows[0]["cname"]);
                        }
                    }
                }

                var iflag = this.DBAccess.ExecuteScalar(string.Format("SELECT iflag FROM {0}_d1 (nolock) where cbilid=@cbilid", sTablename), this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                if (ConvertHelper.ToString(iflag) == "100")
                {
                    response.IsError = true;
                    response.ErrorMessage = "此" + sTablecname + "[" + cbilid + "]已过账！";
                    return response;
                }
                //PDA未入库的，需要删除明细
                string id1s = "";
                var findRow2 = this.DBCache.Get("sy_biltypecfg").Select("cbiltype='" + sBilType + "' and ISNULL(csaveprocname,'')<>''").FirstOrDefault();
                string sumfield = "";
                string prconame = "";
                if (findRow2 != null)
                {
                    sumfield = findRow2["csumfield"].ToString();
                    prconame = findRow2["csaveprocname"].ToString();
                }
                Transaction tran = null;
                //tran = this.DBAccess.CreateTransaction();
                var dt = this.DBAccess.GetDataTable(string.Format("SELECT * FROM {0}_d1 (nolock) where cbilid=@cbilid", sTablename), this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                dt.TableName = sTablename + "_d1";

                #region 组合条码
                DataTable dt12 = null;
                DataTable dt13 = null;
                if (this.DBCache.Get("sy_config").Select("cparmname='INSTRCODE' and cparmvalue='1'").Length > 0)//KB023 UDI保存
                {
                    //V1.2
                    var isNewFlow = (ConvertHelper.ToInt(this.DBAccess.ExecuteScalar("SELECT TOP 1 cparmvalue from sy_config where cparmname='UseZsmNewProcess'")) == 1);
                    var d12FrontTablename = isNewFlow ? "bl_out" : sTablename;

                    #region 保存D12表
                    if (request.LRInfoTrace12List != null)// && request.LRInfoTrace12List.Count > 0 这个不能加，如果存在删除码操作这里就不会被正确执行
                    {
                        if (this.DBCache.Get("sy_dt_tablelist").Select("tablename='" + d12FrontTablename + "_d12'").Length > 0)
                        {
                            if (isNewFlow)
                            {
                                //V1.2 CJJ PDA采码改造
                                //先把本单追溯码的数据取下来放到缓存中,因为主页面中只有新的UDI采集数据（UDI码可重复采集，无需在后续校验）
                                dt12 = this.DBAccess.GetDataTable(string.Format("SELECT * FROM {0}_d12(nolock) where cbilid=@cbilid AND cdatatype = 'ZSM'", d12FrontTablename), this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                                dt12.TableName = d12FrontTablename + "_d12";

                                if (request.LRInfoTrace12List.Count <= 0) dt12.Clear();

                                this.DBAccess.ExecuteNonQuery(string.Format("delete from {0}_d12 where cbilid=@cbilid", d12FrontTablename), this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                            }
                            else
                            {
                                dt12 = this.DBAccess.GetDataTable(string.Format("SELECT * FROM {0}_d12(nolock) where cbilid=@cbilid", d12FrontTablename), this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                                dt12.TableName = d12FrontTablename + "_d12";

                                this.DBAccess.ExecuteNonQuery(string.Format("delete from {0}_d12 where cbilid=@cbilid", d12FrontTablename), this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                            }


                            var maxID12 = dt12.Compute("MAX(id12)", "");

                            if (maxID12 == null || maxID12 == DBNull.Value)
                            {
                                maxID12 = 0;
                            }
                            foreach (var item12 in request.LRInfoTrace12List)
                            {
                                maxID12 = Convert.ToInt32(maxID12) + 1;
                                DataRow newRow = null;
                                newRow = dt12.NewRow();

                                //V1.2
                                if (dt12 != null && dt12.Select().Length > 0 && dt12.Select(string.Format("ctracename='{0}' AND cdatatype = 'ZSM'", item12.ctracename)).Any())
                                {
                                    var originDataRow = dt12.Select(string.Format("ctracename='{0}'", item12.ctracename)).FirstOrDefault();
                                    if (originDataRow != null)
                                    {
                                        //这里涉及到一个行状态的问题
                                        originDataRow.SetAdded();
                                    }
                                    
                                    //SysLog.WriteLocalLog("旧数据重新添加到dt12,Begin", "PDA改造日志！！！");
                                    //dt12.Rows.Add(newRow);
                                    //SysLog.WriteLocalLog("旧数据重新添加到dt12,End", "PDA改造日志！！！");
                                    continue;
                                }


                                DataRowHelper.SetDefaultValue(newRow);
                                newRow["cbilid"] = cbilid;
                                newRow["cbiltype"] = sBilType;
                                newRow["id1"] = item12.id1;
                                newRow["id12"] = maxID12;
                                newRow["cgoodsid"] = item12.cgoodsid;
                                newRow["cph"] = item12.cph;

                                if (isNewFlow)
                                {
                                    newRow["cdatatype"] = "UDI";
                                    newRow["ireplenishflag"] = 0;
                                    newRow["fbaseqty"] = item12.fzsmqty;
                                    newRow["clastupdnote"] = "PDA采集";
                                }

                                if (!string.IsNullOrWhiteSpace(item12.dmadedate))
                                {
                                    newRow["dmadedate"] = Convert.ToDateTime(item12.dmadedate);
                                }
                                else
                                {
                                    newRow["dmadedate"] = DBNull.Value;
                                }
                                if (!string.IsNullOrWhiteSpace(item12.dexpdate))
                                {
                                    newRow["dexpdate"] = Convert.ToDateTime(item12.dexpdate);
                                }
                                else
                                {
                                    newRow["dexpdate"] = DBNull.Value;
                                }
                                newRow["cmjph"] = item12.cmjph;
                                if (!string.IsNullOrWhiteSpace(item12.dmjdate))
                                {
                                    newRow["dmjdate"] = Convert.ToDateTime(item12.dmjdate);
                                }
                                else
                                {
                                    newRow["dmjdate"] = DBNull.Value;
                                }
                                if (!string.IsNullOrWhiteSpace(item12.dmjexpdate))
                                {
                                    newRow["dmjexpdate"] = Convert.ToDateTime(item12.dmjexpdate);
                                }
                                else
                                {
                                    newRow["dmjexpdate"] = DBNull.Value;
                                }

                                newRow["cparenttrace"] = item12.cparenttrace;//父级追溯码，记录完整UDI码
                                newRow["ctracename"] = item12.ctracename;
                                if (newRow.Table.Columns.Contains("cdicode"))
                                {
                                    newRow["cdicode"] = item12.cdicode;//UDI码的 DI码
                                }
                                newRow["corgid"] = item12.corgid;
                                newRow["dscandate"] = DateTime.Now;
                                newRow["cirter"] = request.EmpCode;
                                newRow["fzsmqty"] = item12.fzsmqty;
                                dt12.Rows.Add(newRow);

                            }
                            //if(!this.DBAccess.Save(dt12, tran))
                            //{
                            //    tran.RollBack();
                            //    response.IsError = true;
                            //    response.ErrorMessage = "保存" + sTablecname + "-数据子表["+d12FrontTablename+"_d12]失败!";
                            //    return response;
                            //}
                        }
                    }
                    #endregion
                    #region 保存D13表
                    if (!isNewFlow)
                    {
                        if (request.LRInfoTrace13List != null && request.LRInfoTrace13List.Count > 0)
                        {
                            if (this.DBCache.Get("sy_dt_tablelist").Select("tablename='bl_xcstock_d13'").Length > 0)
                            {
                                this.DBAccess.ExecuteNonQuery(string.Format("delete from {0}_d13 where cbilid=@cbilid", sTablename), this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                                dt13 = this.DBAccess.GetDataTable(string.Format("SELECT * FROM {0}_d13(nolock) where cbilid=@cbilid", sTablename), this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                                dt13.TableName = sTablename + "_d13";

                                var maxID13 = dt13.Compute("MAX(id13)", "");

                                if (maxID13 == null || maxID13 == DBNull.Value)
                                {
                                    maxID13 = 0;
                                }
                                foreach (var item13 in request.LRInfoTrace13List)
                                {
                                    maxID13 = Convert.ToInt32(maxID13) + 1;
                                    DataRow newRow = null;
                                    newRow = dt13.NewRow();
                                    DataRowHelper.SetDefaultValue(newRow);
                                    newRow["id1"] = item13.id1;
                                    newRow["id13"] = maxID13;
                                    newRow["cbilid"] = cbilid;
                                    newRow["cgoodsid"] = item13.cgoodsid;
                                    newRow["cph"] = item13.cph;
                                    if (!string.IsNullOrWhiteSpace(item13.dmadedate))
                                    {
                                        newRow["dmadedate"] = Convert.ToDateTime(item13.dmadedate);
                                    }
                                    else
                                    {
                                        newRow["dmadedate"] = DBNull.Value;
                                    }
                                    if (!string.IsNullOrWhiteSpace(item13.dexpdate))
                                    {
                                        newRow["dexpdate"] = Convert.ToDateTime(item13.dexpdate);
                                    }
                                    else
                                    {
                                        newRow["dexpdate"] = DBNull.Value;
                                    }
                                    newRow["cmjph"] = item13.cmjph;
                                    if (!string.IsNullOrWhiteSpace(item13.dmjdate))
                                    {
                                        newRow["dmjdate"] = Convert.ToDateTime(item13.dmjdate);
                                    }
                                    else
                                    {
                                        newRow["dmjdate"] = DBNull.Value;
                                    }
                                    if (!string.IsNullOrWhiteSpace(item13.dmjexpdate))
                                    {
                                        newRow["dmjexpdate"] = Convert.ToDateTime(item13.dmjexpdate);
                                    }
                                    else
                                    {
                                        newRow["dmjexpdate"] = DBNull.Value;
                                    }
                                    newRow["ctracename"] = item13.ctracename;
                                    newRow["cirter"] = request.EmpCode;
                                    newRow["dirtdate"] = DateTime.Now;
                                    newRow["falotqty"] = item13.falotqty;
                                    newRow["cnote"] = item13.cnote;
                                    dt13.Rows.Add(newRow);

                                }
                                //if (!this.DBAccess.Save(dt13, tran))
                                //{
                                //    tran.RollBack();
                                //    response.IsError = true;
                                //    response.ErrorMessage = "保存" + sTablecname + "-数据子表[" + sTablename + "_d13]失败!";
                                //    return response;
                                //}
                            }
                        }
                    }
                    #endregion
                }
                #endregion
                //tran.Commit();

                //这里处理了追溯码的保存 会按参数ISOUTSTOCKAUTOACC判断是否自动过账
                var request2 = new BusinessRequest() { BusinessKey = "UpdateWMSPickreChkDetailGoodsProcess" };
                request2.Parameters["cfher1"] = request.EmpCode;
                request2.Parameters["cfher2"] = (string.IsNullOrEmpty(request.cfher2) ? "" : request.cfher2); ;
                request2.Parameters["cbilid"] = cbilid;
                var lstGoods = new List<string>();
                foreach (var row in dt.Select(""))
                {
                    lstGoods.Add(string.Format("{0};{1};{2}", row["id1"], row["fqty"] == DBNull.Value ? 0 : (decimal)row["fqty"], row.Table.Columns.Contains("ccancelseason") ? row["ccancelseason"].ToString() : ""));
                }
                request2.Parameters["goods"] = lstGoods;
                request2.Parameters["d12"] = dt12;
                //V1.2 
                if (ConvertHelper.ToInt(this.DBAccess.ExecuteScalar("SELECT TOP 1 cparmvalue from sy_config where cparmname='UseZsmNewProcess'")) != 1)
                {
                    request2.Parameters["d13"] = dt13;
                }

                request2.LoginState = request.LoginState;
                if (request2.LoginState != null) request2.LoginState.EmployeeCode = request.EmpCode;
                if (this.LoginInfo != null && this.LoginInfo.EmpCode == null) this.LoginInfo.EmpCode = request.EmpCode;

                SaveD12(request2, ref response);
            }
            else
            {
                response.IsError = true;
                response.ErrorMessage = "接口类型调用失败，请检查!";
                return response;
            }

            return response;
        }
        private void SumBillHead(DataTable head, DataTable body)
        {
            if (this.DBCache.Get("sy_config").Select("cparmname='SOFTUSERTYPE' and cparmvalue='YSH'").Length > 0)
            {
                return;
            }


            var configRows = this.DBCache.Get("sy_config").Select("cparmname in ('PRICEDECLEN','QTYDECLEN','RATEDECLEN','TAXVALUEDECLEN','VALUEDECLEN','PKQTYDECLEN','NTPRICEDECLEN','EXRATEDECLEN')");

            //fhead_costvalue
            //fhead_discvalue
            //fhead_invvalue
            //fhead_normvalue
            //fhead_notaxcostvalue
            //fhead_notaxvalue
            //fhead_profit
            //fhead_qty
            //fhead_value
            //fhead_ybvalue
            var valueFormat = "F" + configRows.FirstOrDefault(r => r["cparmname"].ToString() == "VALUEDECLEN")["cparmvalue"];
            // var rateFormat = "F" + configRows.FirstOrDefault(r => r["cparmname"].ToString() == "RATEDECLEN")["cparmvalue"];
            var priceFormat = "F12";
            foreach (var sumColumn in head.Columns.Cast<DataColumn>().Where(c => c.ColumnName.StartsWith("fhead_")))
            {
                var sumColumnName = sumColumn.ColumnName.Replace("fhead_", "f");
                if (body.Columns.Contains(sumColumnName))
                {
                    decimal value = 0;
                    foreach (var item in body.Select())
                    {
                        value += Convert.ToDecimal(Convert.ToDecimal(item[sumColumnName] == DBNull.Value ? 0M : item[sumColumnName]).ToString(sumColumnName.EndsWith("value") ? valueFormat : priceFormat));
                    }
                    head.Rows[0][sumColumn] = value;
                }
            }
        }

        //V1.2 用于点击采码后现行保存UDI码采集的数据
        protected void SaveD12(BusinessRequest request, ref PDAXCSTOCKOPResponse response)
        {
            //var response = CreateResponse(request);

            var chker1 = request.Parameters["cfher1"].ToString();
            var chker2 = request.Parameters["cfher2"].ToString();
            var cbilid = request.Parameters["cbilid"].ToString();
            var cbiltype = cbilid.Substring(0, 2).ToUpper();
            var headTableName = this.DBCache.Get("sy_biltypecfg").Select("cbiltype='" + cbiltype + "'").FirstOrDefault()["ctabname"].ToString();
            int lzj = DBCache.Get("sy_config").Select("cparmname='SOFTUSERTYPE' AND cparmvalue='K900247'").Length; //来梓君客户
            var Dfhdate = string.Empty;

            if (this.DBAccess.ExecuteScalar("select top 1 iflag from " + headTableName + "(NOLOCK) where cbilid=@cbilid", this.DBAccess.CreateDbParameter("@cbilid", cbilid)).ToString() == "100")
            {
                response.IsError = true;
                response.ErrorMessage = "此出库单已过账！";
                return;
            }


            var d1TableName = headTableName + "_d1";
            var lstGoods = (request.Parameters["goods"] as List<string>).Select(s =>
            {
                var arr = s.Split(';');
                return new { id1 = Convert.ToInt32(arr[0]), fqty = Convert.ToDecimal(arr[1]), ccancelseason = arr.Length > 2 ? arr[2] : "", dfhdatetime = arr.Length > 3 ? arr[3].ToString() : "" };
            }
                    ).ToList();



            if (request.Parameters.ContainsKey("OneKeyCheck"))
            {
                KingBos.Infrastructure.Utility.SysLog.WriteLocalLog("单号：" + cbilid + Environment.NewLine + KingBos.Infrastructure.Utility.JSONSerializer.Serialize(request.Parameters["goods"]), "一键复核日志");
            }

            //追溯码、UDI新流程
            bool UseZsmNewProcess = DBCache.Get("sy_config").Select("cparmname='UseZsmNewProcess' AND cparmvalue='1'").Length > 0;

            var d12 = request.Parameters["d12"] as DataTable;
            if (d12 != null)
            {
                if (UseZsmNewProcess)
                {
                    d12.TableName = "bl_out_d12";
                }
                else
                {
                    d12.TableName = d1TableName + "2";
                }
            }
            try
            {
                SetD12(request, this.DBAccess);
            }
            catch (Exception ex)
            {
                response.IsError = true;
                response.ErrorMessage = ex.Message;
                return;
            }

            var sql = string.Empty;
            var isSuccess = true;
            var tran = this.DBAccess.CreateTransaction();
            try
            {
                if (isSuccess)
                {
                    if (d12 != null)
                    {
                        if (UseZsmNewProcess)
                        {
                            d12.TableName = "bl_out_d12";
                        }
                        else
                        {
                            d12.TableName = d1TableName + "2";
                        }

                        isSuccess = this.DBAccess.Save(d12, tran);
                    }

                }

                if (isSuccess)
                {
                    tran.Commit();
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
            }
        }

        private void SetD12(BusinessRequest request, DataAccess.DataBaseAccess dba)
        {
            //var instrcode = this.DBCache.Get("sy_config").Select("cparmname='INSTRCODE'").FirstOrDefault()["cparmvalue"].ToString();
            //var codeName = instrcode.Equals("1") ? "UDI码序列号" : "防串码";
            var codeName = "";

            var d12 = request.Parameters["d12"] as DataTable;
            if (d12 != null)
            {
                var cbilid = request.Parameters["cbilid"].ToString();
                var maxD12 = Convert.ToInt32(dba.ExecuteScalar("select ISNULL(MAX(id12),0) from " + d12.TableName + "(nolock) where cbilid=@cbilid", dba.CreateDbParameter("@cbilid", cbilid)));
                var lstGoods = (request.Parameters["goods"] as List<string>).Select(s =>
                {
                    var arr = s.Split(';');
                    return new { id1 = Convert.ToInt32(arr[0]), fqty = Convert.ToDecimal(arr[1]) };
                }
               ).ToList();

                var goodsids = string.Join(",", d12.Select().Select(x => "'" + x["cgoodsid"] + "'").ToArray());
                DataTable goods = null;

                if (d12.Select().Any())
                {
                    goods = dba.GetDataTable(string.Format("select cgoodsid,isnull(izsmflag,0) as izsmflag,isnull(iudiflag,0) as iudiflag,isnull(ichinamedicine,0) as ichinamedicine from bf_goods(nolock) where cgoodsid in ({0})", goodsids));
                }

                foreach (DataRow d12Row in d12.Rows.Cast<DataRow>().ToList())
                {
                    var id1 = Convert.ToInt32(d12Row.RowState == DataRowState.Deleted ? d12Row["id1", DataRowVersion.Original] : d12Row["id1"]);
                    if (!lstGoods.Any(g => g.id1 == id1))
                    {
                        d12.Rows.Remove(d12Row);
                    }
                    else
                    {
                        if (d12Row.RowState != DataRowState.Deleted)
                        {
                            d12Row["cbilid"] = cbilid;
                            if (d12Row.RowState == DataRowState.Added)
                            {
                                maxD12++;
                                d12Row["id12"] = maxD12;
                            }
                        }
                    }
                }

                foreach (var item in lstGoods)
                {
                    var id1 = item.id1;
                    var count = 0;
                    DataRow[] findD12Rows = null;
                    var cgoodsid = "";

                    if (d12.Select("id1=" + id1).Any())
                    {
                        cgoodsid = d12.Select("id1=" + id1).FirstOrDefault()["cgoodsid"].ToString();
                    }
                    if (d12.Columns.Contains("iqualifiedflag"))
                    {
                        findD12Rows = d12.Select("id1=" + id1 + " and iqualifiedflag=1 and clastupdnote=''");
                        //count = findD12Rows.Length;
                    }
                    else
                    {
                        findD12Rows = d12.Select("id1=" + id1 + " and  clastupdnote=''");
                        //count = findD12Rows.Length;                      
                    }

                    //49106
                    count = findD12Rows.Sum(x => ConvertHelper.ToInt(x["fzsmqty"]));

                    //48279 DI码去掉扫码数量和单据数量的校验
                    //if (count > 0 && instrcode.Equals("0"))                   
                    //{
                    //    if (count > item.fqty)
                    //    {
                    //        throw new Exception("采集的" + codeName + "数量大于数据行数量。");
                    //    }
                    //}

                    //
                    if (count > item.fqty && goods.Select().Any(x => x["cgoodsid"].ToString() == cgoodsid && Convert.ToInt16(x["ichinamedicine"]) == 0))
                    {
                        if (null != goods && goods.Select().Any(x => x["cgoodsid"].ToString() == cgoodsid && Convert.ToInt16(x["izsmflag"]) == 1))
                        {
                            codeName = "追溯码";
                        }
                        else if (null != goods && goods.Select().Any(x => x["cgoodsid"].ToString() == cgoodsid && Convert.ToInt16(x["iudiflag"]) == 1))
                        {
                            codeName = "UDI码";
                        }
                        else
                        {
                            codeName = "";
                        }

                        if (!string.IsNullOrWhiteSpace(codeName))
                        {
                            throw new Exception("采集的" + codeName + "数量大于数据行数量。");
                        }
                    }
                }
            }
        }

    }
}




