 
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
V1.2        LONG LONG AGO   未知      CREATE
V1.1        2025-07-30      CJJ       增加仓库筛选
V1.2        2025-08-05      CJJ       BUG#59662增加显示生产许可证
V1.3        2025-08-08      CJJ       原先这里查询UDI码不知道写的啥，优化了下去重的逻辑
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

    public class PDASHOPProcess : BaseRequestProcesser<PDASHOPRequest>
    {
        protected override BaseResponse Execute(PDASHOPRequest request)
        {
            //this.LoginInfo.EmpCode = request.EmpCode;
            var response = this.CreateResponse(request);
            if (request.OPType == 0)
            {
                #region 获取收货单列表
                string sql = @"
SELECT bl_shd.cbilid, bl_shd.dbildate, bl_shd.cbiltype, bl_shd.corgid, bl_shd.cdeptid,
bl_shd.cckid, bl_shd.ishtype ,bl_shd.chandler ,bl_shd.csalerid ,bl_shd.ccorpid ,bl_shd.ctagorgid
,bl_shd.crecbilid ,bl_shd.crecer ,bl_shd.dstartdate ,bl_shd.ientrustflag ,bl_shd.ref_cbilid
,bl_shd.ref_cbiltype ,bl_shd.ref_ctabname ,bl_shd.irefrigerateflag ,bl_shd.fhead_qty
,bl_shd.fhead_value ,bl_shd.iflag ,bl_shd.cnote, bf_employee.cempname,bf_store.cckname, bf_org.corgname
,v_bf_corp.ccorpname , v_bf_corp.caddress

FROM bl_shd WITH(NOLOCK) LEFT JOIN bf_org (NOLOCK) ON bl_shd.corgid = bf_org.corgid 
LEFT JOIN bf_store (NOLOCK) ON bl_shd.cckid = bf_store.cckid LEFT JOIN bf_employee (NOLOCK) ON bl_shd.chandler = bf_employee.cempid 
LEFT JOIN v_bf_corp WITH(NOLOCK) ON case bl_shd.ishtype when 1 then 'vender' when 2 then 'cust' when 3 then 'org' else '' end = v_bf_corp.ctype 
AND bl_shd.ccorpid = v_bf_corp.ccorpid
WHERE bl_shd.cbiltype = 'SH' AND (bl_shd.iflag < 100 OR (bl_shd.iflag = 100 AND DATEDIFF(dd,bl_shd.dbildate,GETDATE())<30 )) ";
                if (request.OrgID != null && request.OrgID != "")
                {
                    sql += " AND bl_shd.corgid = '" + request.OrgID + "'";
                }
                //if (request.EmpCode != null && request.EmpCode != "")
                //{
                //    sql += " AND bl_shd.chandler = '" + request.EmpCode + "'";
                //}
                sql += " ORDER BY bl_shd.dbildate DESC, bl_shd.cbilid ";
                var dt = this.DBAccess.GetDataTable(sql);
                response.Result = new List<SHMainInfo>();
                foreach (var item in dt.Select())
                {
                    response.Result.Add(new SHMainInfo()
                    {
                        cbilid = item["cbilid"].ToString(),
                        dbildate = item["dbildate"].ToString(),
                        cbiltype = item["cbiltype"].ToString(),
                        corgid = item["corgid"].ToString(),
                        cdeptid = item["cdeptid"].ToString(),
                        cckid = item["cckid"].ToString(),
                        ishtype = item["ishtype"].ToInt(),
                        chandler = item["chandler"].ToString(),
                        csalerid = item["csalerid"].ToString(),
                        ccorpid = item["ccorpid"].ToString(),
                        ctagorgid = item["ctagorgid"].ToString(),
                        crecbilid = item["crecbilid"].ToString(),
                        crecer = item["crecer"].ToString(),
                        dstartdate = item["dstartdate"].ToString(),//ConvertHelper.ToDateTime(item["dstartdate"]),
                        ientrustflag = item["ientrustflag"].ToInt(),
                        ref_cbilid = item["ref_cbilid"].ToString(),
                        ref_cbiltype = item["ref_cbiltype"].ToString(),
                        ref_ctabname = item["ref_ctabname"].ToString(),
                        irefrigerateflag = item["irefrigerateflag"].ToInt(),
                        fhead_qty = Convert.ToDecimal(item["fhead_qty"] == DBNull.Value ? 0 : item["fhead_qty"]),
                        fhead_value = Convert.ToDecimal(item["fhead_value"] == DBNull.Value ? 0 : item["fhead_value"]),
                        iflag = item["iflag"].ToInt(),
                        cnote = item["cnote"].ToString(),
                        cempname = item["cempname"].ToString(),
                        cckname = item["cckname"].ToString(),
                        corgname = item["corgname"].ToString(),
                        ccorpname = item["ccorpname"].ToString(),
                        caddress = item["caddress"].ToString()
                    });
                }
                #endregion
            }
            else if (request.OPType == 1)
            {
                #region 仓库信息（新建收货单）
                string sql = "SELECT id, cckid, cckname, corgid, itype, iflag, cnote, cirter, dirtdate, clastupder, dlastupdtime, clastupdnote FROM bf_store (NOLOCK) WHERE iflag = 100 ";
                if (request.OrgID != null && request.OrgID != "")
                {
                    sql += " AND bf_store.corgid = '" + request.OrgID + "'";
                }
                var dt = this.DBAccess.GetDataTable(sql);
                response.StoreInfoList = new List<StoreInfo>();
                foreach (DataRow item in dt.Rows)
                {
                    response.StoreInfoList.Add(new StoreInfo()
                    {
                        cckid = item["cckid"].ToString(),
                        cckname = item["cckname"].ToString(),
                        corgid = item["corgid"].ToString(),
                        itype = item["itype"].ToInt(),
                        iflag = item["iflag"].ToInt(),
                        cnote = item["cnote"].ToString(),
                        cirter = item["cirter"].ToString(),
                        dirtdate = ConvertHelper.ToDateTime(item["dirtdate"]),
                        clastupder = item["clastupder"].ToString(),
                        dlastupdtime = ConvertHelper.ToDateTime(item["dlastupdtime"]),
                        clastupdnote = item["clastupdnote"].ToString()
                    });
                }
                #endregion
            }
            else if (request.OPType == 2)
            {
                #region 往来单位信息（新建收货单）
                var blnEnableOrgSYJL = false;
                if (this.DBCache.Get("sy_config").Select("cparmname='EnableOrgSYJL' AND cparmvalue='1'").Length > 0 && !string.IsNullOrEmpty(request.OrgID))//2023-02-18 是否启用集团机构首营
                    blnEnableOrgSYJL = true;

                string sql = "SELECT v.ctype, v.ccorpid, v.ccorpname, v.caddress, v.czjmcode FROM v_bf_corp (NOLOCK) v ";
                if (blnEnableOrgSYJL && request.ishtype == 1)//采购订单
                {
                    sql += " inner join bf_org_vender(nolock) b ON b.cvenderid=v.ccorpid ";
                }
                else if (blnEnableOrgSYJL && request.ishtype == 2)//销售退回申请
                {
                    sql += " inner join bf_org_cust(nolock) b ON b.ccustid=v.ccorpid ";
                }
                sql += " WHERE 1 = 1 AND case " + request.ishtype + " when 1 then 'vender' when 2 then 'cust' when 3 then 'org' else '' end = v.ctype";
                if (request.QueryText != null && request.QueryText != "")
                {
                    sql += " AND (v.ccorpname like '%" + request.QueryText + "%' OR v.czjmcode like '%" + request.QueryText + "%' OR v.ccorpid = '" + request.QueryText + "')";
                }
                if (blnEnableOrgSYJL && (request.ishtype == 1 || request.ishtype == 2))
                {
                    sql += " and b.corgid='" + request.OrgID + "'";
                }

                var dt = this.DBAccess.GetDataTable(sql);
                response.CorpInfoList = new List<CorpInfo>();
                foreach (DataRow item in dt.Rows)
                {
                    response.CorpInfoList.Add(new CorpInfo()
                    {
                        ctype = item["ctype"].ToString(),
                        ccorpid = item["ccorpid"].ToString(),
                        ccorpname = item["ccorpname"].ToString(),
                        caddress = item["caddress"].ToString(),
                        czjmcode = item["czjmcode"].ToString()
                    });
                }
                #endregion
            }
            else if (request.OPType == 3)
            {
                #region 新增收货单
                if (request.ccorpid == null || request.ccorpid == "" || request.cckid == null || request.cckid == ""
                    || request.EmpCode == null || request.EmpCode == "" || request.QueryText == null || request.QueryText == ""
                    )
                {
                    response.IsError = true;
                    response.ErrorMessage = "参数不完整！";
                    return response;
                }
                if (!(request.ishtype > 0 && request.ishtype < 3))
                {
                    response.IsError = true;
                    response.ErrorMessage = "数据来源不正确！";
                    return response;
                }

                var headDT = this.DBAccess.GetDataTable("select * from bl_shd (nolock) where 1=2");
                headDT.TableName = "bl_shd";
                var newHeadRow = headDT.NewRow();
                DataRowHelper.SetDefaultValue(newHeadRow, (r, c) => { return c.DataType != typeof(DateTime); });
                newHeadRow["corgid"] = request.OrgID;
                newHeadRow["cckid"] = request.cckid;
                newHeadRow["chandler"] = request.EmpCode;
                newHeadRow["cirter"] = request.EmpCode;
                newHeadRow["clastupder"] = request.EmpCode;
                //newHeadRow["dstartdate"] = DateTime.Now;
                //newHeadRow["denddate"] = DateTime.Now;
                newHeadRow["dbildate"] = DateTime.Now;
                newHeadRow["cbiltype"] = "SH";
                newHeadRow["iflag"] = 0;
                newHeadRow["cnote"] = "RF手持终端录入";

                newHeadRow["ccorpid"] = request.ccorpid;
                newHeadRow["crecbilid"] = request.QueryText;
                newHeadRow["ishtype"] = request.ishtype;
                headDT.Rows.Add(newHeadRow);

                Transaction tran = null;
                try
                {
                    tran = this.DBAccess.CreateTransaction();
                    var newBillID = DBIdentityBuilder.GetNewBillNO(newHeadRow["cbiltype"].ToString(), newHeadRow["corgid"].ToString(), tran);
                    newHeadRow["cbilid"] = newBillID;
                    this.DBAccess.Save(headDT, tran);
                    tran.Commit();
                    response.ResponseCode = newBillID;

                    string sql = @"
                    SELECT bl_shd.cbilid, bl_shd.dbildate, bl_shd.cbiltype, bl_shd.corgid, bl_shd.cdeptid,
                    bl_shd.cckid, bl_shd.ishtype ,bl_shd.chandler ,bl_shd.csalerid ,bl_shd.ccorpid ,bl_shd.ctagorgid
                    ,bl_shd.crecbilid ,bl_shd.crecer ,bl_shd.dstartdate ,bl_shd.ientrustflag ,bl_shd.ref_cbilid
                    ,bl_shd.ref_cbiltype ,bl_shd.ref_ctabname ,bl_shd.irefrigerateflag ,bl_shd.fhead_qty
                    ,bl_shd.fhead_value ,bl_shd.iflag ,bl_shd.cnote, bf_employee.cempname,bf_store.cckname, bf_org.corgname
                    ,v_bf_corp.ccorpname , v_bf_corp.caddress

                    FROM bl_shd WITH(NOLOCK) LEFT JOIN bf_org (NOLOCK) ON bl_shd.corgid = bf_org.corgid 
                    LEFT JOIN bf_store (NOLOCK) ON bl_shd.cckid = bf_store.cckid LEFT JOIN bf_employee (NOLOCK) ON bl_shd.chandler = bf_employee.cempid 
                    LEFT JOIN v_bf_corp WITH(NOLOCK) ON case bl_shd.ishtype when 1 then 'vender' when 2 then 'cust' when 3 then 'org' else '' end = v_bf_corp.ctype 
                    AND bl_shd.ccorpid = v_bf_corp.ccorpid

                    WHERE bl_shd.cbilid = '" + newBillID + "'";
                    var dt = this.DBAccess.GetDataTable(sql);
                    response.Result = new List<SHMainInfo>();
                    foreach (DataRow item in dt.Rows)
                    {
                        response.Result.Add(new SHMainInfo()
                        {
                            cbilid = item["cbilid"].ToString(),
                            dbildate = item["dbildate"].ToString(),
                            cbiltype = item["cbiltype"].ToString(),
                            corgid = item["corgid"].ToString(),
                            cdeptid = item["cdeptid"].ToString(),
                            cckid = item["cckid"].ToString(),
                            ishtype = item["ishtype"].ToInt(),
                            chandler = item["chandler"].ToString(),
                            csalerid = item["csalerid"].ToString(),
                            ccorpid = item["ccorpid"].ToString(),
                            ctagorgid = item["ctagorgid"].ToString(),
                            crecbilid = item["crecbilid"].ToString(),
                            crecer = item["crecer"].ToString(),
                            dstartdate = item["dstartdate"].ToString(),//ConvertHelper.ToDateTime(item["dstartdate"]),
                            ientrustflag = item["ientrustflag"].ToInt(),
                            ref_cbilid = item["ref_cbilid"].ToString(),
                            ref_cbiltype = item["ref_cbiltype"].ToString(),
                            ref_ctabname = item["ref_ctabname"].ToString(),
                            irefrigerateflag = item["irefrigerateflag"].ToInt(),
                            fhead_qty = Convert.ToDecimal(item["fhead_qty"] == DBNull.Value ? 0 : item["fhead_qty"]),
                            fhead_value = Convert.ToDecimal(item["fhead_value"] == DBNull.Value ? 0 : item["fhead_value"]),
                            iflag = item["iflag"].ToInt(),
                            cnote = item["cnote"].ToString(),
                            cempname = item["cempname"].ToString(),
                            cckname = item["cckname"].ToString(),
                            corgname = item["corgname"].ToString(),
                            ccorpname = item["ccorpname"].ToString(),
                            caddress = item["caddress"].ToString()
                        });
                    }
                }
                catch (Exception ex)
                {
                    if (tran != null)
                    {
                        tran.RollBack();
                    }
                    response.IsError = true;
                    response.ErrorMessage = ex.ToString();
                }
                #endregion
            }
            else if (request.OPType == 4)
            {
            	//V1.1
                //V1.2
                #region 收货单获取明细列表
                string sql = @"
DECLARE @cempid varchar(100)
SELECT top 1 @cempid = cempid FROM sy_user(nolock) where cempname = @crecerID

select * from 
(
select * from 
(
SELECT 2 as ishtype,bl_xsthsq.ccustid AS ccorpid,v_bf_cust1.ccustname AS ccorpname,bl_xsthsq.csalerid,
bl_xsthsq_d1.id1,bl_xsthsq_d1.cbilid,bl_xsthsq.dbildate,
bl_xsthsq_d1.cph,bl_xsthsq_d1.dmadedate,bl_xsthsq_d1.dexpdate,bl_xsthsq_d1.cphnote,bl_xsthsq_d1.cphnote2,bl_xsthsq_d1.cmjph,bl_xsthsq_d1.dmjdate,bl_xsthsq_d1.dmjexpdate,bl_xsthsq_d1.cmjphnote,

bl_xsthsq_d1.cgoodsid,v_bf_goods0.cgoodsname as cgoodsid_v_cgoodsname,v_bf_goods0.ccommonname as cgoodsid_v_ccommonname,v_bf_goods0.cpkname as cgoodsid_v_cpkname,v_bf_goods0.cprodaddress as cgoodsid_v_cprodaddress,v_bf_goods0.cfactoryname as cgoodsid_v_cfactoryname,v_bf_goods0.cfileno as cgoodsid_v_cfileno,v_bf_goods0.cunit as cgoodsid_v_cunit,
v_bf_goods0.iphflag as cgoodsid_v_iphflag,v_bf_goods0.imjphflag as cgoodsid_v_imjphflag,v_bf_goods0.izsmflag as cgoodsid_v_izsmflag,v_bf_goods0.cbarcode, v_bf_goods0.cbarcode1, v_bf_goods0.cbarcode2, v_bf_goods0.isgspcold, v_bf_goods0.iterm, v_bf_goods0.czjmcode as cgoodsid_v_czjmcode,
ISNULL(v_bf_goods0.fpklong,0) as fpklong, ISNULL(v_bf_goods0.fpkwidth,0) As fpkwidth ,ISNULL(v_bf_goods0.fpkheight,0) As fpkheight, ISNULL(v_bf_goods0.fpkvolume,0) As fpkvolume, ISNULL(v_bf_goods0.fpkweight,0) As fpkweight,

bl_xsthsq_d1.fltp,bl_xsthsq_d1.fmtp,bl_xsthsq_d1.flqty,bl_xsthsq_d1.fmqty,bl_xsthsq_d1.fsqty,bl_xsthsq_d1.frecqty,
bl_xsthsq_d1.cjzyl1,bl_xsthsq_d1.cjzyl2,bl_xsthsq_d1.corgid,bl_xsthsq_d1.cckid,bl_xsthsq_d1.ctagorgid,bl_xsthsq_d1.cbatchid,
bl_xsthsq_d1.fqty-bl_xsthsq_d1.fturnqty-isnull(ftmpqty,0)-isnull(ftmprecrejectqty,0)  AS fqty,
bl_xsthsq_d1.fqty-bl_xsthsq_d1.fturnqty-isnull(ftmpqty,0)-isnull(ftmprecrejectqty,0)  AS chg_ref_fqty,
bl_xsthsq_d1.fqty-bl_xsthsq_d1.fturnqty-isnull(ftmpqty,0)-isnull(ftmprecrejectqty,0)  AS fsumqty,
bl_xsthsq_d1.fnormprice,(bl_xsthsq_d1.fqty-bl_xsthsq_d1.fturnqty-isnull(ftmpqty,0)-isnull(ftmprecrejectqty,0) )*bl_xsthsq_d1.fnormprice as fnormvalue,
bl_xsthsq_d1.fdiscrate,
bl_xsthsq_d1.fprice,(bl_xsthsq_d1.fqty-bl_xsthsq_d1.fturnqty-isnull(ftmpqty,0)-isnull(ftmprecrejectqty,0) )*bl_xsthsq_d1.fprice as fvalue,
bl_xsthsq_d1.fdiscvalue,bl_xsthsq_d1.ftaxrate,bl_xsthsq_d1.fnotaxprice,bl_xsthsq_d1.fnotaxvalue,bl_xsthsq_d1.ftaxvalue,bl_xsthsq_d1.fbzrate,bl_xsthsq_d1.fybprice,bl_xsthsq_d1.fybvalue,bl_xsthsq_d1.finvprice,bl_xsthsq_d1.finvvalue,bl_xsthsq_d1.cbz,bl_xsthsq_d1.iflag,bl_xsthsq_d1.isortid,

bl_xsthsq_d1.cbilid AS orig_cbilid,bl_xsthsq.cbiltype AS orig_cbiltype,'bl_xsthsq_d1' AS orig_ctabname,bl_xsthsq_d1.id1 as orig_iid,
bl_xsthsq_d1.cbilid AS chg_ref_cbilid,bl_xsthsq.cbiltype AS chg_ref_cbiltype,'bl_xsthsq_d1' AS chg_ref_ctabname,bl_xsthsq_d1.id1 AS chg_ref_iid,bl_xsthsq_d1.cnote,
bl_xsthsq.ientrustflag,bf_goods_group.cgroupitemid AS cgoodsgroupid ,
bf_org_goodsplace.cjzcwcode +'/'+bf_org_goodsplace.ccwcode AS chwcode,
bl_xsthsq_d1.cphcmaidcode
,CASE WHEN ISNULL(bl_xsthsq_d1.cckid,'') <> '' THEN bl_xsthsq_d1.cckid ELSE bl_xsthsq.cckid END as ckcode
,v_bf_goods0.ccertificateno

FROM bl_xsthsq_d1
LEFT JOIN bl_xsthsq ON bl_xsthsq_d1.cbilid = bl_xsthsq.cbilid
LEFT JOIN 
(
select ref_cbilid,ref_iid,sum(case when fqty >= 0 then fqty when cnote = @h_cbilid and fqty<0 then fqty else 0 end) as ftmpqty,sum(frecrejectqty) as ftmprecrejectqty
from bl_shd_d2 
group by ref_cbilid,ref_iid
) d2
ON bl_xsthsq_d1.cbilid = d2.ref_cbilid and bl_xsthsq_d1.id1 = d2.ref_iid
LEFT JOIN v_bf_goods (nolock) as v_bf_goods0 on v_bf_goods0.cgoodsid=bl_xsthsq_d1.cgoodsid
LEFT JOIN v_bf_cust (nolock) as v_bf_cust1 on v_bf_cust1.ccustid=bl_xsthsq.ccustid
LEFT JOIN bf_goods_group(NOLOCK) AS bf_goods_group ON bf_goods_group.cgoodsid = bl_xsthsq_d1.cgoodsid AND bf_goods_group.cgroupid = (SELECT cparmvalue FROM dbo.sy_config WHERE cparmname='AutoSplitBillGroup')
LEFT JOIN bf_org_goodsplace(NOLOCK) bf_org_goodsplace  ON bf_org_goodsplace.cgoodsid=bl_xsthsq_d1.cgoodsid
WHERE bl_xsthsq.iflag = 100 
and bl_xsthsq.ifinish=0
and bl_xsthsq_d1.fqty-bl_xsthsq_d1.fturnqty-isnull(ftmpqty,0)-isnull(ftmprecrejectqty,0)  > 0
and 
(
not exists(select 1 from bl_shd_d2 where ref_cbilid = bl_xsthsq_d1.cbilid and ref_iid = bl_xsthsq_d1.id1)
or
exists(select 1 from bl_shd_d2 where ref_cbilid = bl_xsthsq_d1.cbilid and ref_iid = bl_xsthsq_d1.id1 and bl_shd_d2.crecer = @cempid)
)
--and exists(select 1 from bf_org org where org.corgid = bl_xsthsq.corgid and org.cblorgid = @in_corgid)
and bl_xsthsq.corgid = @h_corgid


UNION ALL

SELECT 1 as ishtype,bl_cgdd.cvenderid AS ccorpid,v_bf_vender1.cvendername AS ccorpname,bl_cgdd.csalerid,
bl_cgdd_d1.id1,bl_cgdd_d1.cbilid,bl_cgdd.dbildate,
'' as cph,null as dmadedate,null as dexpdate,'' as cphnote,'' as cphnote2,'' as cmjph,null as dmjdate,null as dmjexpdate,'' as cmjphnote,

bl_cgdd_d1.cgoodsid,v_bf_goods0.cgoodsname as cgoodsid_v_cgoodsname,v_bf_goods0.ccommonname as cgoodsid_v_ccommonname,v_bf_goods0.cpkname as cgoodsid_v_cpkname,v_bf_goods0.cprodaddress as cgoodsid_v_cprodaddress,v_bf_goods0.cfactoryname as cgoodsid_v_cfactoryname,v_bf_goods0.cfileno as cgoodsid_v_cfileno,v_bf_goods0.cunit as cgoodsid_v_cunit,
v_bf_goods0.iphflag as cgoodsid_v_iphflag,v_bf_goods0.imjphflag as cgoodsid_v_imjphflag,v_bf_goods0.izsmflag as cgoodsid_v_izsmflag,v_bf_goods0.cbarcode, v_bf_goods0.cbarcode1, v_bf_goods0.cbarcode2, v_bf_goods0.isgspcold, v_bf_goods0.iterm, v_bf_goods0.czjmcode as cgoodsid_v_czjmcode,
ISNULL(v_bf_goods0.fpklong,0) as fpklong, ISNULL(v_bf_goods0.fpkwidth,0) As fpkwidth ,ISNULL(v_bf_goods0.fpkheight,0) As fpkheight, ISNULL(v_bf_goods0.fpkvolume,0) As fpkvolume, ISNULL(v_bf_goods0.fpkweight,0) As fpkweight,

bl_cgdd_d1.fltp,bl_cgdd_d1.fmtp,bl_cgdd_d1.flqty,bl_cgdd_d1.fmqty,bl_cgdd_d1.fsqty,bl_cgdd_d1.frecqty,
bl_cgdd_d1.cjzyl1,bl_cgdd_d1.cjzyl2,bl_cgdd_d1.corgid,bl_cgdd_d1.cckid,bl_cgdd_d1.ctagorgid,bl_cgdd_d1.cbatchid,
bl_cgdd_d1.fqty-bl_cgdd_d1.fturnqty-isnull(ftmpqty,0)-isnull(ftmprecrejectqty,0) AS fqty,
bl_cgdd_d1.fqty-bl_cgdd_d1.fturnqty-isnull(ftmpqty,0)-isnull(ftmprecrejectqty,0) AS chg_ref_fqty,
bl_cgdd_d1.fqty-bl_cgdd_d1.fturnqty-isnull(ftmpqty,0)-isnull(ftmprecrejectqty,0) AS fsumqty,
bl_cgdd_d1.fnormprice,(bl_cgdd_d1.fqty-bl_cgdd_d1.fturnqty-isnull(ftmpqty,0)-isnull(ftmprecrejectqty,0))*bl_cgdd_d1.fnormprice as fnormvalue,
bl_cgdd_d1.fdiscrate,
bl_cgdd_d1.fprice,(bl_cgdd_d1.fqty-bl_cgdd_d1.fturnqty-isnull(ftmpqty,0)-isnull(ftmprecrejectqty,0))*bl_cgdd_d1.fprice as fvalue,
bl_cgdd_d1.fdiscvalue,bl_cgdd_d1.ftaxrate,bl_cgdd_d1.fnotaxprice,bl_cgdd_d1.fnotaxvalue,bl_cgdd_d1.ftaxvalue,bl_cgdd_d1.fbzrate,bl_cgdd_d1.fybprice,bl_cgdd_d1.fybvalue,bl_cgdd_d1.finvprice,bl_cgdd_d1.finvvalue,bl_cgdd_d1.cbz,bl_cgdd_d1.iflag,bl_cgdd_d1.isortid,

bl_cgdd_d1.cbilid AS orig_cbilid,bl_cgdd.cbiltype AS orig_cbiltype,'bl_cgdd_d1' AS orig_ctabname,bl_cgdd_d1.id1 as orig_iid,
bl_cgdd_d1.cbilid as chg_ref_cbilid,bl_cgdd.cbiltype AS chg_ref_cbiltype,'bl_cgdd_d1' AS chg_ref_ctabname,bl_cgdd_d1.id1 AS chg_ref_iid,bl_cgdd_d1.cnote,  
bl_cgdd.ientrustflag,bf_goods_group.cgroupitemid AS cgoodsgroupid,
bf_org_goodsplace.cjzcwcode +'/'+bf_org_goodsplace.ccwcode AS chwcode,
bl_cgdd_d1.cphcmaidcode
,CASE WHEN ISNULL(bl_cgdd_d1.cckid,'') <> '' THEN bl_cgdd_d1.cckid ELSE bl_cgdd.cckid END as ckcode
,v_bf_goods0.ccertificateno

FROM bl_cgdd_d1
LEFT JOIN bl_cgdd ON bl_cgdd_d1.cbilid = bl_cgdd.cbilid
LEFT JOIN 
(
select ref_cbilid,ref_iid,sum(case when fqty >= 0 then fqty when cnote = @h_cbilid and fqty<0 then fqty else 0 end) as ftmpqty,sum(frecrejectqty) as ftmprecrejectqty
from bl_shd_d2 
group by ref_cbilid,ref_iid
) d2
ON bl_cgdd_d1.cbilid = d2.ref_cbilid and bl_cgdd_d1.id1 = d2.ref_iid
LEFT JOIN v_bf_goods (nolock) as v_bf_goods0 on v_bf_goods0.cgoodsid=bl_cgdd_d1.cgoodsid
LEFT JOIN v_bf_vender (nolock) as v_bf_vender1 on v_bf_vender1.cvenderid=bl_cgdd.cvenderid
LEFT JOIN bf_goods_group(NOLOCK) AS bf_goods_group ON bf_goods_group.cgoodsid = bl_cgdd_d1.cgoodsid AND bf_goods_group.cgroupid = (SELECT cparmvalue FROM dbo.sy_config WHERE cparmname='AutoSplitBillGroup')
LEFT JOIN bf_org_goodsplace(NOLOCK) bf_org_goodsplace  ON bf_org_goodsplace.cgoodsid=bl_cgdd_d1.cgoodsid
WHERE bl_cgdd.iflag = 100 
and bl_cgdd.ifinish=0
and bl_cgdd_d1.fqty-bl_cgdd_d1.fturnqty-isnull(ftmpqty,0)-isnull(ftmprecrejectqty,0) > 0
and 
(
not exists(select 1 from bl_shd_d2 where ref_cbilid = bl_cgdd_d1.cbilid and ref_iid = bl_cgdd_d1.id1)
or
exists(select 1 from bl_shd_d2 where ref_cbilid = bl_cgdd_d1.cbilid and ref_iid = bl_cgdd_d1.id1 and bl_shd_d2.crecer = @cempid)
)
--and exists(select 1 from bf_org org where org.corgid = bl_cgdd.corgid and org.cblorgid = @in_corgid)
and bl_cgdd.corgid = @h_corgid

UNION ALL

SELECT 3 as ishtype,bl_mdthsq.caskorgid AS ccorpid,v_bf_org1.corgname AS ccorpname,bl_mdthsq.csalerid,
bl_mdthsq_d1.id1,bl_mdthsq_d1.cbilid,bl_mdthsq.dbildate,
bl_mdthsq_d1.cph,bl_mdthsq_d1.dmadedate,bl_mdthsq_d1.dexpdate,bl_mdthsq_d1.cphnote,bl_mdthsq_d1.cphnote2,bl_mdthsq_d1.cmjph,bl_mdthsq_d1.dmjdate,bl_mdthsq_d1.dmjexpdate,bl_mdthsq_d1.cmjphnote,

bl_mdthsq_d1.cgoodsid,v_bf_goods0.cgoodsname as cgoodsid_v_cgoodsname,v_bf_goods0.ccommonname as cgoodsid_v_ccommonname,v_bf_goods0.cpkname as cgoodsid_v_cpkname,v_bf_goods0.cprodaddress as cgoodsid_v_cprodaddress,v_bf_goods0.cfactoryname as cgoodsid_v_cfactoryname,v_bf_goods0.cfileno as cgoodsid_v_cfileno,v_bf_goods0.cunit as cgoodsid_v_cunit,
v_bf_goods0.iphflag as cgoodsid_v_iphflag,v_bf_goods0.imjphflag as cgoodsid_v_imjphflag,v_bf_goods0.izsmflag as cgoodsid_v_izsmflag,v_bf_goods0.cbarcode, v_bf_goods0.cbarcode1, v_bf_goods0.cbarcode2, v_bf_goods0.isgspcold, v_bf_goods0.iterm, v_bf_goods0.czjmcode as cgoodsid_v_czjmcode,
ISNULL(v_bf_goods0.fpklong,0) as fpklong, ISNULL(v_bf_goods0.fpkwidth,0) As fpkwidth ,ISNULL(v_bf_goods0.fpkheight,0) As fpkheight, ISNULL(v_bf_goods0.fpkvolume,0) As fpkvolume, ISNULL(v_bf_goods0.fpkweight,0) As fpkweight,

bl_mdthsq_d1.fltp,bl_mdthsq_d1.fmtp,bl_mdthsq_d1.flqty,bl_mdthsq_d1.fmqty,bl_mdthsq_d1.fsqty,bl_mdthsq_d1.frecqty,
bl_mdthsq_d1.cjzyl1,bl_mdthsq_d1.cjzyl2,bl_mdthsq_d1.corgid,bl_mdthsq_d1.cckid,bl_mdthsq_d1.ctagorgid,bl_mdthsq_d1.cbatchid,
bl_mdthsq_d1.fqty-bl_mdthsq_d1.fturnqty-isnull(ftmpqty,0)-isnull(ftmprecrejectqty,0) AS fqty,
bl_mdthsq_d1.fqty-bl_mdthsq_d1.fturnqty-isnull(ftmpqty,0)-isnull(ftmprecrejectqty,0) AS chg_ref_fqty,
bl_mdthsq_d1.fqty-bl_mdthsq_d1.fturnqty-isnull(ftmpqty,0)-isnull(ftmprecrejectqty,0) AS fsumqty,
bl_mdthsq_d1.fnormprice,(bl_mdthsq_d1.fqty-bl_mdthsq_d1.fturnqty-isnull(ftmpqty,0)-isnull(ftmprecrejectqty,0))*bl_mdthsq_d1.fnormprice as fnormvalue,
bl_mdthsq_d1.fdiscrate,
bl_mdthsq_d1.fprice,(bl_mdthsq_d1.fqty-bl_mdthsq_d1.fturnqty-isnull(ftmpqty,0)-isnull(ftmprecrejectqty,0))*bl_mdthsq_d1.fprice as fvalue,
bl_mdthsq_d1.fdiscvalue,bl_mdthsq_d1.ftaxrate,bl_mdthsq_d1.fnotaxprice,bl_mdthsq_d1.fnotaxvalue,bl_mdthsq_d1.ftaxvalue,bl_mdthsq_d1.fbzrate,bl_mdthsq_d1.fybprice,bl_mdthsq_d1.fybvalue,bl_mdthsq_d1.finvprice,bl_mdthsq_d1.finvvalue,bl_mdthsq_d1.cbz,bl_mdthsq_d1.iflag,bl_mdthsq_d1.isortid,

bl_mdthsq_d1.cbilid AS orig_cbilid,bl_mdthsq.cbiltype AS orig_cbiltype,'bl_mdthsq_d1' AS orig_ctabname,bl_mdthsq_d1.id1 as orig_iid,
bl_mdthsq_d1.cbilid AS chg_ref_cbilid,bl_mdthsq.cbiltype AS chg_ref_cbiltype,'bl_mdthsq_d1' AS chg_ref_ctabname,bl_mdthsq_d1.id1 AS chg_ref_iid,bl_mdthsq_d1.cnote,  
bl_mdthsq.ientrustflag,bf_goods_group.cgroupitemid AS cgoodsgroupid,
bf_org_goodsplace.cjzcwcode +'/'+bf_org_goodsplace.ccwcode AS chwcode,
bl_mdthsq_d1.cphcmaidcode
,'' as ckcode
,v_bf_goods0.ccertificateno

FROM bl_mdthsq_d1
LEFT JOIN bl_mdthsq ON bl_mdthsq_d1.cbilid = bl_mdthsq.cbilid
LEFT JOIN 
(
select ref_cbilid,ref_iid,sum(case when fqty >= 0 then fqty when cnote = @h_cbilid and fqty<0 then fqty else 0 end) as ftmpqty,sum(frecrejectqty) as ftmprecrejectqty
from bl_shd_d2 
group by ref_cbilid,ref_iid
) d2
ON bl_mdthsq_d1.cbilid = d2.ref_cbilid and bl_mdthsq_d1.id1 = d2.ref_iid
LEFT JOIN v_bf_goods (nolock) as v_bf_goods0 on v_bf_goods0.cgoodsid=bl_mdthsq_d1.cgoodsid
LEFT JOIN v_bf_org (nolock) as v_bf_org1 on v_bf_org1.corgid=bl_mdthsq.caskorgid
LEFT JOIN bf_goods_group(NOLOCK) AS bf_goods_group ON bf_goods_group.cgoodsid = bl_mdthsq_d1.cgoodsid AND bf_goods_group.cgroupid = (SELECT cparmvalue FROM dbo.sy_config WHERE cparmname='AutoSplitBillGroup')
LEFT JOIN bf_org_goodsplace(NOLOCK) bf_org_goodsplace  ON bf_org_goodsplace.cgoodsid=bl_mdthsq_d1.cgoodsid
WHERE bl_mdthsq.iflag = 100
and bl_mdthsq.ifinish=0
and bl_mdthsq_d1.fqty-bl_mdthsq_d1.fturnqty-isnull(ftmpqty,0)-isnull(ftmprecrejectqty,0) > 0
and 
(
not exists(select 1 from bl_shd_d2 where ref_cbilid = bl_mdthsq_d1.cbilid and ref_iid = bl_mdthsq_d1.id1)
or
exists(select 1 from bl_shd_d2 where ref_cbilid = bl_mdthsq_d1.cbilid and ref_iid = bl_mdthsq_d1.id1 and bl_shd_d2.crecer = @cempid)
)
and exists(select 1 from bf_org org where org.corgid = bl_mdthsq.corgid and org.cblorgid = @in_corgid)
--and bl_mdthsq.corgid = @h_corgid


) as bl_cgdd_d1
where ishtype = @ishtype and ccorpid = @ccorpid and (ckcode = @cckid OR ISNULL(ckcode,'')='')

) as bl_cgdd_d1
WHERE 1 = 1
 AND (bl_cgdd_d1.cgoodsid like '%'+ @QueryText  +'%' OR bl_cgdd_d1.cgoodsid_v_cgoodsname like '%'+ @QueryText  +'%' OR bl_cgdd_d1.cgoodsid_v_ccommonname like '%'+ @QueryText  +'%' OR bl_cgdd_d1.cgoodsid_v_czjmcode like '%'+ @QueryText  +'%' 
        OR bl_cgdd_d1.cbarcode like '%'+ @QueryText  +'%' OR bl_cgdd_d1.cbarcode1 like '%'+ @QueryText  +'%' OR bl_cgdd_d1.cbarcode2 like '%'+ @QueryText  +'%' 
)

ORDER BY bl_cgdd_d1.dbildate DESC
";
                var pdashGetddtype = ConvertHelper.ToString(this.DBCache.Get("sy_config").Select("cparmname ='PDASHGETDDTYPE'").FirstOrDefault()["cparmvalue"]);
                if (pdashGetddtype == "1")
                {
                    sql = sql.Replace("ORDER BY bl_cgdd_d1.dbildate DESC", "ORDER BY bl_cgdd_d1.dbildate ASC");
                }
                var dt = this.DBAccess.GetDataTable(sql, this.DBAccess.CreateDbParameter("@in_corgid", request.OrgID)
                	, this.DBAccess.CreateDbParameter("@cckid", request.cckid)
                    , this.DBAccess.CreateDbParameter("@ishtype", request.ishtype), this.DBAccess.CreateDbParameter("@ccorpid", request.ccorpid)
                    , this.DBAccess.CreateDbParameter("@crecerID", request.EmpCode), this.DBAccess.CreateDbParameter("@h_corgid", request.OrgID)
                    , this.DBAccess.CreateDbParameter("@h_cbilid", request.cbilid), this.DBAccess.CreateDbParameter("@QueryText", request.QueryText));
                var IsEnableSFDARenewal = this.DBCache.Get("sy_config").Select("cparmname='IsEnableSFDARenewal' and cparmvalue='1'").Length > 0;//是否启用注册证换证功能
                response.SHGoodsList = new List<SHGoods>();
                foreach (var item in dt.Select())
                {
                    var info = new SHGoods()
                    {
                        cgoodsid = item["cgoodsid"].ToString(),
                        cgoodsname = item["cgoodsid_v_cgoodsname"].ToString(),
                        cbarcode = item["cbarcode"].ToString(),
                        cprodaddress = item["cgoodsid_v_cprodaddress"].ToString(),
                        cfileno = item["cgoodsid_v_cfileno"].ToString(),
                        cpkname = item["cgoodsid_v_cpkname"].ToString(),
                        cfactoryname = item["cgoodsid_v_cfactoryname"].ToString(),
                        cunit = item["cgoodsid_v_cunit"].ToString(),
                        iphflag = item["cgoodsid_v_iphflag"].ToInt(),
                        czjmcode = item["cgoodsid_v_czjmcode"].ToString(),
                        fpklong = Convert.ToDecimal(item["fpklong"]),
                        fpkwidth = Convert.ToDecimal(item["fpkwidth"]),
                        fpkheight = Convert.ToDecimal(item["fpkheight"]),
                        fpkvolume = Convert.ToDecimal(item["fpkvolume"]),
                        fpkweight = Convert.ToDecimal(item["fpkweight"]),
                        cph = item["cph"].ToString(),
                        cphnote2 = item["cphnote2"].ToString(),
                        ishtype = item["ishtype"].ToInt(),
                        dmadedate = item["dmadedate"].ToString(),
                        dexpdate = item["dexpdate"].ToString(),
                        ccorpid = item["ccorpid"].ToString(),
                        ccorpname = item["ccorpname"].ToString(),
                        cbilid = request.cbilid,
                        dbildate = item["dbildate"].ToString(),
                        id1 = item["id1"].ToInt(),
                        fqty = Convert.ToDecimal(item["fqty"] == DBNull.Value ? 0 : item["fqty"]),
                        ref_fqty = Convert.ToDecimal(item["fqty"] == DBNull.Value ? 0 : item["fqty"]),
                        ref_fprice = Convert.ToDecimal(item["fnormprice"] == DBNull.Value ? 0 : item["fnormprice"]),
                        fnormprice = Convert.ToDecimal(item["fnormprice"] == DBNull.Value ? 0 : item["fnormprice"]),
                        fnormvalue = Convert.ToDecimal(item["fnormvalue"] == DBNull.Value ? 0 : item["fnormvalue"]),
                        cnote = item["cnote"].ToString(),
                        cbiltype = item["chg_ref_cbiltype"].ToString(),
                        isgspcold = item["isgspcold"].ToInt(),
                        iterm = item["iterm"].ToInt(),
                        fdiscrate = Convert.ToDecimal(item["fdiscrate"] == DBNull.Value ? 0 : item["fdiscrate"]),
                        fdiscvalue = Convert.ToDecimal(item["fdiscvalue"] == DBNull.Value ? 0 : item["fdiscvalue"]),
                        fprice = Convert.ToDecimal(item["fprice"] == DBNull.Value ? 0 : item["fprice"]),
                        fvalue = Convert.ToDecimal(item["fvalue"] == DBNull.Value ? 0 : item["fvalue"]),
                        ftaxrate = Convert.ToDecimal(item["ftaxrate"] == DBNull.Value ? 0 : item["ftaxrate"]),
                        ftaxvalue = Convert.ToDecimal(item["ftaxvalue"] == DBNull.Value ? 0 : item["ftaxvalue"]),
                        fnotaxprice = Convert.ToDecimal(item["fnotaxprice"] == DBNull.Value ? 0 : item["fnotaxprice"]),
                        fnotaxvalue = Convert.ToDecimal(item["fnotaxvalue"] == DBNull.Value ? 0 : item["fnotaxvalue"]),
                        finvprice = Convert.ToDecimal(item["finvprice"] == DBNull.Value ? 0 : item["finvprice"]),
                        finvvalue = Convert.ToDecimal(item["finvvalue"] == DBNull.Value ? 0 : item["finvvalue"]),
                        cbz = item["cbz"].ToString(),
                        orig_ctabname = item["orig_ctabname"].ToString(),
                        orig_cbiltype = item["orig_cbiltype"].ToString(),
                        orig_cbilid = item["orig_cbilid"].ToString(),
                        orig_iid = item["orig_iid"].ToString(),
                        ref_cbilid = item["chg_ref_cbilid"].ToString(),
                        ref_cbiltype = item["chg_ref_cbiltype"].ToString(),
                        ref_ctabname = item["chg_ref_ctabname"].ToString(),
                        ref_iid = item["chg_ref_iid"].ToString(),
                        csendadd = "",
                        dsendstart = "",
                        drectime = "",
                        fhours = 0,
                        ctep = "",
                        csendtype = "",
                        ctepctl = "",
                        ctepnote = "",
                        ctepsta = "",
                        ccar = "",
                        ccarno = "",
                        csender = "",
                        ctranser = "",
                        ctransport = "",
                        cdap = "",
                        csdap = "",
                        cstep = "",
                        chwcode = item["chwcode"].ToString(),
                        frecqty = ConvertHelper.ToDecimal(item["frecqty"]),
                        cphcmaidcode = item["cphcmaidcode"].ToString(),
                        PHList = new List<SHPHInfo>(),
                        ccertificateno = ConvertHelper.ToString(item["ccertificateno"]),//V1.2
                        cfilenofilelist = new List<PDAGoodscfilenofilelistRequest>()
                    };
                    var phDT = this.DBAccess.GetDataTable(@"
                    select id, corgid, cgoodsid, cph, dmadedate, dexpdate, cphnote,cphnote2, iflag, cnote, cirter, dirtdate, clastupder, dlastupdtime, clastupdnote
                    from bf_ph_info(nolock) 
                    where cgoodsid = @cgoodsid

                    ", this.DBAccess.CreateDbParameter("@cgoodsid", info.cgoodsid));
                    foreach (DataRow phRow in phDT.Rows)
                    {
                        var ph = new SHPHInfo()
                        {
                            cph = phRow["cph"].ToString(),
                            cgoodsid = info.cgoodsid,
                            cgoodsname = info.cgoodsname,
                            cphnote2 = phRow["cphnote2"].ToString(),
                            cmjph = "",
                            dmadedate = phRow["dmadedate"] == DBNull.Value ? "" : Convert.ToDateTime(phRow["dmadedate"]).ToString("yyyy-MM-dd"),
                            dexpdate = phRow["dexpdate"] == DBNull.Value ? "" : Convert.ToDateTime(phRow["dexpdate"]).ToString("yyyy-MM-dd"),
                            fqty = info.fqty,
                            iphflag = info.iphflag,
                            iterm = info.iterm
                        };
                        info.PHList.Add(ph);
                    }
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
                    response.SHGoodsList.Add(info);
                }

                #region UDI明细
                if (this.DBCache.Get("sy_config").Select("cparmname='INSTRCODE' and cparmvalue='1'").Length > 0)//启用了UDI参数
                {
                    if (this.DBCache.Get("sy_dt_tablelist").Select("tablename='bl_shd_d12'").Length > 0)
                    {
                        response.LRInfoTrace12List = new List<GoodsTrace12>();

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
                            sql = "select * from bl_shd_d12 where cbilid=@cbilid";
                        }

                        
                        //sql = "select * from bl_shd_d12 where cbilid=@cbilid";
                        dt = this.DBAccess.GetDataTable(sql, this.DBAccess.CreateDbParameter("@cbilid", request.cbilid));
                        foreach (var item12 in dt.Select())
                        {
                            var findRow12 = response.LRInfoTrace12List.Where(p => p.cgoodsid == item12["cgoodsid"].ToString() && p.id1 == ConvertHelper.ToInt(item12["id1"])
                                                && p.ctracename == item12["ctracename"].ToString() && p.cph == item12["cph"].ToString()).FirstOrDefault();
                            if (findRow12 == null)
                            {
                                var item1 = response.SHGoodsList.Where(a => a.cgoodsid == item12["cgoodsid"].ToString() && a.cph == item12["cph"].ToString() && (ConvertHelper.ToString(item12["cnote"]) == ConvertHelper.ToString(a.ref_cbilid)) && ConvertHelper.ToInt(a.id1) == ConvertHelper.ToInt(item12["id1"])).FirstOrDefault();
                                var GoodsTrace12 = new GoodsTrace12()
                                {

                                    //var item1 = phInfo.Where(a => a.cgoodsid == item12.cgoodsid && a.cph == item12.cph && (ConvertHelper.ToString(item12.cbilid) == ConvertHelper.ToString(info.ref_cbilid)) && ConvertHelper.ToInt(info.ref_iid) == ConvertHelper.ToInt(item12.id1)).FirstOrDefault();
                                    cbilid = (item1 != null) ? item1.ref_cbilid : ConvertHelper.ToString(item12["cbilid"]),
                                    id1 = (item1 != null) ? ConvertHelper.ToInt(item1.ref_iid) : ConvertHelper.ToInt(item12["id1"]),
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
                                    fzsmqty = ConvertHelper.ToDecimal(item12["fzsmqty"]),
                                    cnote = ConvertHelper.ToString(item12["cnote"])
                                };
                                response.LRInfoTrace12List.Add(GoodsTrace12);
                            }
                        }
                    }
                    if (this.DBCache.Get("sy_dt_tablelist").Select("tablename='bl_shd_d13'").Length > 0)
                    {
                        response.LRInfoTrace13List = new List<GoodsTrace13>();
                        sql = "select * from bl_shd_d13 where cbilid=@cbilid";
                        dt = this.DBAccess.GetDataTable(sql, this.DBAccess.CreateDbParameter("@cbilid", request.cbilid));
                        foreach (var item13 in dt.Select())
                        {
                            var findRow12 = response.LRInfoTrace12List.Where(p => p.cgoodsid == item13["cgoodsid"].ToString() && p.id1 == ConvertHelper.ToInt(item13["id1"])
                                                && p.ctracename == item13["ctracename"].ToString() && p.cph == item13["cph"].ToString()).FirstOrDefault();
                            if (findRow12 == null)
                            {
                                var item1 = response.SHGoodsList.Where(a => a.cgoodsid == item13["cgoodsid"].ToString() && a.cph == item13["cph"].ToString() && (ConvertHelper.ToString(item13["cnote"]) == ConvertHelper.ToString(a.ref_cbilid)) && ConvertHelper.ToInt(a.id1) == ConvertHelper.ToInt(item13["id1"])).FirstOrDefault();
                                var GoodsTrace13 = new GoodsTrace13()
                                {
                                    cbilid = (item1 != null) ? item1.ref_cbilid : ConvertHelper.ToString(item13["cbilid"]),
                                    id1 = (item1 != null) ? ConvertHelper.ToInt(item1.ref_iid) : ConvertHelper.ToInt(item13["id1"]),
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
                #endregion
            }
            else if (request.OPType == 5)
            {
                #region 拒绝原因列表
                string sql = "SELECT ccodevalue, ccodetext, cnote FROM sy_lookup(nolock) WHERE ctype ='REJECTIONREASON' ";
                var dt = this.DBAccess.GetDataTable(sql);
                response.RejectionReasonsList = new List<RejectionReason>();
                foreach (DataRow item in dt.Rows)
                {
                    response.RejectionReasonsList.Add(new RejectionReason()
                    {
                        ccodevalue = item["ccodevalue"].ToString(),
                        ccodetext = item["ccodetext"].ToString(),
                        cnote = item["cnote"].ToString()
                    });
                }
                #endregion
            }
            else if (request.OPType == 6)
            {
                if (request.cbilid == null || request.cbilid == "")
                {
                    response.IsError = true;
                    response.ErrorMessage = "参数不完整！";
                    return response;
                }
                Transaction tran = null;
                tran = this.DBAccess.CreateTransaction();
                var cbilid = request.cbilid;
                var corgid = this.DBAccess.ExecuteScalar(@"SELECT TOP 1 corgid FROM bl_shd(NOLOCK) WHERE cbilid=@cbilid",
                  this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                var toopBarProcResponse = this.ExecuteOther(
                   new BillToolBarProcActionRequest()
                   {
                       ModuleID = "DMSM01001210",
                       BillNO = cbilid,
                       BillType = "SH",
                       UserID = request.EmpCode,
                       ActionType = "del", //function.ActionType,
                       Action = "del-ok", // overwriteAction != null ? overwriteAction : function.Action,
                       Note = "",
                       OrgID = corgid.ToString(),
                       Cirter = request.EmpCode,
                       BillTypeName = "", // xop.WindowsTitle,
                       transaction = tran,
                       async = false,
                       MenuCode = "",//2018-7-30 ZGH
                       MenuName = "",
                       InvokeType = 0
                   }
                   );
                if (toopBarProcResponse.IsError || !toopBarProcResponse.Result.Success)
                {
                    tran.RollBack();
                }
                else
                {

                    tran.Commit();
                }
                #region 作废收货单
                //if (request.cbilid == null || request.cbilid == "")
                //{
                //    response.IsError = true;
                //    response.ErrorMessage = "参数不完整！";
                //    return response;
                //}
                //var count = this.DBAccess.ExecuteNonQuery("update bl_shd set iflag=999 where cbilid=@cbilid and iflag>=0 AND iflag<100", this.DBAccess.CreateDbParameter("@cbilid", request.cbilid));
                //if (count == 0)
                //{
                //    response.IsError = true;
                //    response.ErrorMessage = "作废失败，受影响行数为0，请刷新数据！";
                //    return response;
                //}
                #endregion
            }
            else if (request.OPType == 7)
            {
                #region 保存收货单
                if (request.SHInfo == null || request.LRInfoList == null)
                {
                    response.IsError = true;
                    response.ErrorMessage = "参数不完整！";
                    return response;
                }

                if (request.OrgID == null || request.OrgID == "")
                {
                    response.IsError = true;
                    response.ErrorMessage = "参数不完整！";
                    return response;
                }

                bool IsColdGoods = false;
                if (request.ColdStore != null)
                {
                    IsColdGoods = true;
                }

                //var cbilid = this.DBAccess.ExecuteScalar(@"SELECT TOP 1 cbilid FROM bl_shd(NOLOCK) WHERE cbilid=@cbilid",
                //   this.DBAccess.CreateDbParameter("@cbilid", request.cbilid));
                //if (cbilid == null)
                //{
                //    response.IsError = true;
                //    response.ErrorMessage = "收货单单据（" + request.cbilid + "）不存在！";
                //    return response;
                //}
                #region 源单业务组
                //1:采购订单;2:销售退回申请;3:门店退回申请
                var refsql = "";
                var bENABLESALERMODE = (this.DBCache.Get("sy_config").Select("cparmname='ENABLESALERMODE' and cparmvalue='1'").Length > 0);
                var bEnableBodySalerid = (this.DBCache.Get("sy_config").Select("cparmname='EnableBodySalerid' and cparmvalue='1'").Length > 0);
                if (bENABLESALERMODE && request.SHInfo.ishtype == 1)
                {
                    if (bEnableBodySalerid)
                        refsql = "select cbilid,csalerid,cgoodsid,id1  from bl_cgdd_d1(nolock) where isnull(csalerid,'')<>'' and cbilid+'~'+CONVERT(VARCHAR(32),id1) in({0}) ";
                    else
                        refsql = "select cbilid,csalerid from bl_cgdd(nolock) where isnull(csalerid,'')<>'' and cbilid in({0}) ";
                }
                else if (bENABLESALERMODE && request.SHInfo.ishtype == 2)
                {
                    if (bEnableBodySalerid)
                        refsql = "select cbilid,csalerid,cgoodsid,id1  from bl_xsthsq_d1(nolock) where isnull(csalerid,'')<>'' and cbilid+'~'+CONVERT(VARCHAR(32),id1) in({0}) ";
                    else
                        refsql = "select cbilid,csalerid  from bl_xsthsq(nolock) where isnull(csalerid,'')<>'' and cbilid  in({0}) ";
                }
                else if (bENABLESALERMODE && request.SHInfo.ishtype == 3)
                {
                    if (bEnableBodySalerid)
                        refsql = "select cbilid,csalerid,cgoodsid,id1  from bl_mdthsq_d1(nolock) where isnull(csalerid,'')<>'' and cbilid+'~'+CONVERT(VARCHAR(32),id1) in({0}) ";
                    else
                        refsql = "select cbilid,csalerid  from bl_mdthsq(nolock) where isnull(csalerid,'')<>'' and cbilid in({0}) ";
                }
                if (!string.IsNullOrEmpty(refsql))
                {
                    //= tryPayInput.drugdetail.GroupBy(g => g.medins_list_codg).Aggregate("''", (c, r) => c + ",'" + r.Key + "'");
                    var sVal = "";
                    if (bEnableBodySalerid)
                        sVal=request.LRInfoList.GroupBy(w => w.ref_cbilid + "~" + w.ref_iid).Aggregate("''", (c, r) => c + ",'" + r.Key + "'");
                    else
                        sVal = request.LRInfoList.GroupBy(w => w.ref_cbilid).Aggregate("''", (c, r) => c + ",'" + r.Key + "'");
                    refsql = string.Format(refsql, sVal);
                }

                var refdt = !string.IsNullOrEmpty(refsql) ? this.DBAccess.GetDataTable(refsql) : null;
                #endregion
                Transaction tran = null;
                tran = this.DBAccess.CreateTransaction();
                var cbilid = request.SHInfo.cbilid;
                if (request.SHInfo.cbilid == "Add")
                {
                    #region 新增单据
                    var headDT = this.DBAccess.GetDataTable("select * from bl_shd (nolock) where 1=2");
                    headDT.TableName = "bl_shd";
                    var newHeadRow = headDT.NewRow();
                    DataRowHelper.SetDefaultValue(newHeadRow, (r, c) => { return c.DataType != typeof(DateTime); });
                    newHeadRow["corgid"] = request.OrgID;
                    newHeadRow["cckid"] = request.SHInfo.cckid;
                    newHeadRow["chandler"] = request.EmpCode;
                    newHeadRow["cirter"] = request.EmpCode;
                    newHeadRow["clastupder"] = request.EmpCode;
                    //newHeadRow["dstartdate"] = DateTime.Now;
                    //newHeadRow["denddate"] = DateTime.Now;
                    newHeadRow["crecer"] = request.EmpCode;
                    newHeadRow["dbildate"] = DateTime.Now;
                    newHeadRow["cbiltype"] = "SH";
                    newHeadRow["iflag"] = 0;
                    newHeadRow["cnote"] = "RF手持终端录入";

                    newHeadRow["ccorpid"] = request.SHInfo.ccorpid;
                    newHeadRow["crecbilid"] = request.SHInfo.crecbilid;
                    newHeadRow["ishtype"] = request.SHInfo.ishtype;
                    if (newHeadRow.Table.Columns.Contains("csalerid") && refdt != null && refdt.Rows.Count > 0)
                    {
                        newHeadRow["csalerid"] = refdt.Select("csalerid<>''").FirstOrDefault()["csalerid"].ToString();
                    }
                    headDT.Rows.Add(newHeadRow);

                    try
                    {
                        var newBillID = DBIdentityBuilder.GetNewBillNO(newHeadRow["cbiltype"].ToString(), newHeadRow["corgid"].ToString(), tran);
                        newHeadRow["cbilid"] = newBillID;
                        this.DBAccess.Save(headDT, tran);
                        response.ResponseCode = newBillID;
                        cbilid = newBillID;

                    }
                    catch (Exception ex)
                    {
                        if (tran != null)
                        {
                            tran.RollBack();
                        }
                        response.IsError = true;
                        response.ErrorMessage = ex.ToString();
                        goto retrueResponse;
                    }
                    #endregion
                }
                try
                {
                    #region 保存明细记录
                    var cckid = this.DBAccess.ExecuteScalar(@"SELECT TOP 1 cckid FROM bl_shd(NOLOCK) WHERE cbilid=@cbilid", tran,
                       this.DBAccess.CreateDbParameter("@cbilid", cbilid));

                    var corgid = this.DBAccess.ExecuteScalar(@"SELECT TOP 1 corgid FROM bl_shd(NOLOCK) WHERE cbilid=@cbilid", tran,
                       this.DBAccess.CreateDbParameter("@cbilid", cbilid));

                    this.DBAccess.ExecuteNonQuery("delete from bl_shd_d1 where cbilid=@cbilid", tran, this.DBAccess.CreateDbParameter("@cbilid", cbilid));

                    DataSet ds = new DataSet();
                    var dt = this.DBAccess.GetDataTable("SELECT * FROM bl_shd_d1(nolock) where cbilid=@cbilid", tran, this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                    dt.TableName = "bl_shd_d1";
                    ds.Tables.Add(dt);

                    var maxID1 = dt.Compute("MAX(id1)", "");

                    if (maxID1 == null || maxID1 == DBNull.Value)
                    {
                        maxID1 = 0;
                    }
                    var d1maxID1 = maxID1;
                    bool bINSTRCODE = false;
                    var bHaveTabled12 = false;
                    var bHaveTabled13 = false;
                    DataTable dt12 = new DataTable();
                    DataTable dt13 = new DataTable();
                    bINSTRCODE = (this.DBCache.Get("sy_config").Select("cparmname='INSTRCODE' and cparmvalue='1'").Length > 0);
                    var IsUseZsmNewProcess = ConvertHelper.ToInt(this.DBAccess.ExecuteScalar("SELECT TOP 1 cparmvalue from sy_config where cparmname='UseZsmNewProcess'"))==1;//是否启用新UDI流程
                    if (bINSTRCODE)
                    {
                        if (IsUseZsmNewProcess)
                        {
                            bHaveTabled12 = (this.DBCache.Get("sy_dt_tablelist").Select("tablename='bl_rk_d12'").Length > 0);
                            if (bHaveTabled12)
                            {
                                //20250528 CJJ PDA采码改造
                                //先把本单追溯码的数据取下来放到缓存中（UDI码可重复采集，无需在后续校验）
                                dt12 = this.DBAccess.GetDataTable("SELECT * FROM bl_rk_d12(nolock) where cbilid=@cbilid AND cdatatype = 'ZSM'", tran, this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                                dt12.TableName = "bl_rk_d12";

                                //再做删除
                                this.DBAccess.ExecuteNonQuery("delete from bl_rk_d12 where cbilid=@cbilid", tran, this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                            }
                        }
                        else
                        {
                            bHaveTabled12 = (this.DBCache.Get("sy_dt_tablelist").Select("tablename='bl_shd_d12'").Length > 0);
                            if (bHaveTabled12)
                            {
                                this.DBAccess.ExecuteNonQuery("delete from bl_shd_d12 where cbilid=@cbilid", tran, this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                                dt12 = this.DBAccess.GetDataTable("SELECT * FROM bl_shd_d12(nolock) where cbilid=@cbilid", tran, this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                                dt12.TableName = "bl_shd_d12";

                            }
                            bHaveTabled13 = (this.DBCache.Get("sy_dt_tablelist").Select("tablename='bl_shd_d13'").Length > 0);
                            if (bHaveTabled13)
                            {
                                this.DBAccess.ExecuteNonQuery("delete from bl_shd_d13 where cbilid=@cbilid", tran, this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                                dt13 = this.DBAccess.GetDataTable("SELECT * FROM bl_shd_d13(nolock) where cbilid=@cbilid", tran, this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                                dt13.TableName = "bl_shd_d13";
                            }
                        }
                    }
                    var IsEnableSFDARenewal = this.DBCache.Get("sy_config").Select("cparmname='IsEnableSFDARenewal' and cparmvalue='1'").Length > 0;//是否启用注册证换证功能

                    var configRows = this.DBCache.Get("sy_config").Select("cparmname in ('PRICEDECLEN','QTYDECLEN','RATEDECLEN','TAXVALUEDECLEN','VALUEDECLEN','PKQTYDECLEN','NTPRICEDECLEN','EXRATEDECLEN')");
                    var valueFormatlen = configRows.FirstOrDefault(r => r["cparmname"].ToString() == "VALUEDECLEN")["cparmvalue"];
                    var strvalueFormatlen = "";
                    if (ConvertHelper.ToString(valueFormatlen) == "")
                    {
                        strvalueFormatlen = "F6";
                    }
                    else
                    {
                        strvalueFormatlen = "F" + valueFormatlen;
                    }
                    var iSamePHProcess = ConvertHelper.ToInt(this.DBCache.Get("sy_config").Select("cparmname='SamePHProcess'").FirstOrDefault()["cparmvalue"]);
                    foreach (var info in request.LRInfoList)
                    {
                        DataRow row = null;
                        var phInfo = info.PHList;
                        #region PDA删除收货数据时处理
                        if (!(phInfo != null && phInfo.Count > 0))
                        {
                            if (!string.IsNullOrEmpty(info.ref_cbilid) && info.ref_cbilid != "-" && !string.IsNullOrEmpty(info.ref_ctabname) && info.ref_ctabname == "bl_cgdd_d1")
                            {
                                var sql = @"update  bl_cgdd_d1 set 
	                        fturnqty= ISNULL(CASE WHEN sh.fqty+sh.frecrejectqty>bl_cgdd_d1.fqty THEN bl_cgdd_d1.fqty ELSE sh.fqty+sh.frecrejectqty END,0), 
	                        frecqty= ISNULL(CASE WHEN sh.fqty>bl_cgdd_d1.fqty THEN bl_cgdd_d1.fqty ELSE sh.fqty END,0) ,
	                        frecvalue= ROUND(ISNULL(CASE WHEN sh.fqty>bl_cgdd_d1.fqty THEN bl_cgdd_d1.fqty ELSE sh.fqty END,0)*fprice, convert(int,@valuedeclen)),
	                        frecrejectqty=ISNULL(sh.frecrejectqty,0),
	                        frecrejectvalue= ROUND(ISNULL(sh.frecrejectqty,0)*fprice,convert(int,@valuedeclen))
                        FROM bl_cgdd_d1
                        LEFT JOIN 
                        (SELECT bl_shd_d1.ref_cbilid,bl_shd_d1.ref_iid,
	                        SUM(fqty) AS fqty,SUM(case when bl_shd.iflag<100 then frecrejectqty else 0 end)+ISNULL(MAX(js.fcancelqty),0) AS frecrejectqty
	                        FROM bl_shd(nolock) 
	                        INNER JOIN bl_shd_d1(nolock) ON bl_shd_d1.cbilid = bl_shd.cbilid
	                        LEFT JOIN (
	                           SELECT orig_cbilid,orig_iid,SUM(fcancelqty) AS fcancelqty
	                           FROM 
	                           (
		                           SELECT d1.orig_cbilid,d1.orig_iid,SUM(case when bt.iflag>=100 THEN 0 ELSE d1.fqty end) AS fcancelqty 
		                           FROM dbo.bl_jsd(nolock) bt 
		                           INNER JOIN dbo.bl_jsd_d1(nolock) d1 ON d1.cbilid = bt.cbilid and d1.ref_cbiltype='SH'
		                           WHERE EXISTS(SELECT 1 FROM bl_shd_d1(nolock) p1 INNER JOIN bl_shd p ON p.cbilid = p1.cbilid 
						                        WHERE p.iflag = 100 AND p1.orig_cbilid = d1.orig_cbilid AND p1.orig_iid = d1.orig_iid
						                        and p1.orig_cbilid =@in_refcbilid and p1.orig_iid=@in_refiid
						                        )
		                           GROUP BY d1.orig_cbilid,d1.orig_iid
		                           UNION ALL
		                           SELECT d1.orig_cbilid,d1.orig_iid,SUM(d1.fqty) AS fcancelqty 
		                           FROM dbo.bl_jsd(nolock) bt 
		                           INNER JOIN dbo.bl_jsd_d1(nolock) d1 ON d1.cbilid = bt.cbilid and d1.ref_cbiltype='SH'
		                           WHERE bt.iflag = 100 
		                           AND EXISTS(SELECT 1 FROM bl_shd_d1(nolock) p1 WHERE p1.orig_cbilid = d1.orig_cbilid AND p1.orig_iid = d1.orig_iid
						                        and p1.orig_cbilid =@in_refcbilid and p1.orig_iid=@in_refiid
		                           )
		                           GROUP BY d1.orig_cbilid,d1.orig_iid
	                           )js1 GROUP BY orig_cbilid,orig_iid
	                        ) js ON js.orig_cbilid = bl_shd_d1.ref_cbilid AND js.orig_iid = bl_shd_d1.ref_iid
	                        WHERE bl_shd.ishtype = 1 AND bl_shd.iflag <> 999
	                           AND bl_shd_d1.cbilid<>@in_cbilid 
	                           AND bl_shd_d1.ref_cbilid =@in_refcbilid and bl_shd_d1.orig_iid=@in_refiid
	                        GROUP BY bl_shd_d1.ref_cbilid,bl_shd_d1.ref_iid
                        ) sh 
                        on bl_cgdd_d1.cbilid = sh.ref_cbilid AND bl_cgdd_d1.id1 = sh.ref_iid
                        WHERE cbilid=@in_refcbilid and id1=@in_refiid";
                                this.DBAccess.ExecuteNonQuery(sql, tran
                                    , this.DBAccess.CreateDbParameter("@in_reftabname", info.ref_ctabname)
                                    , this.DBAccess.CreateDbParameter("@in_fqty", info.fqty)
                                    , this.DBAccess.CreateDbParameter("@valuedeclen", valueFormatlen)
                                    , this.DBAccess.CreateDbParameter("@in_refcbilid", info.ref_cbilid)
                                    , this.DBAccess.CreateDbParameter("@in_refiid", info.ref_iid)
                                    , this.DBAccess.CreateDbParameter("@in_cbilid", cbilid));
                            }
                        }
                        #endregion

                        #region 保存d1的ph

                        foreach (var phitem in phInfo)
                        {
                            //var findRow = dt.Select(string.Format("cgoodsid='{0}' and ref_cbilid='{1}' and ref_iid='{2}' and cph='{3}' "
                            //                        , info.cgoodsid, info.ref_cbilid, info.orig_iid, phitem.cph)).FirstOrDefault();

                            //if (findRow == null)
                            //{
                            #region 保存时校验数量
                            if (request.SHInfo.cbilid == "Add" && !string.IsNullOrEmpty(info.ref_ctabname) && !string.IsNullOrEmpty(info.ref_cbilid))
                            {
                                var ref_fqty = 0M;//通知数量
                                var fshqty = 0M; //收货数量
                                var fjsqty = 0M;//拒收数量
                                var sql = "";
                                fshqty = phInfo.Sum(p => p.fshqty);
                                fjsqty = phInfo.Sum(p => p.fqsqty);

                                sql = "select (fqty-fturnqty) as ref_fqty from {0} (nolock) where cbilid='{1}' and id1= {2}";
                                ref_fqty = ConvertHelper.ToDecimal(this.DBAccess.ExecuteScalar(string.Format(sql, info.ref_ctabname, info.ref_cbilid, info.ref_iid), tran));
                                if (fshqty + fjsqty > ref_fqty)
                                {
                                    if (tran != null)
                                    {
                                        tran.RollBack();
                                    }
                                    response.IsError = true;
                                    response.ErrorMessage = "商品[" + info.cgoodsname + "] 收货数量[" + Convert.ToDecimal(fshqty).ToString("F2") + "]+拒收数量[" + fjsqty.ToString("F2") + "]不能大于通知数量[" + ref_fqty.ToString("F2") + "]";
                                    goto retrueResponse;
                                }
                            }
                            #endregion
                            maxID1 = Convert.ToInt32(maxID1) + 1;
                            d1maxID1 = maxID1;
                            if (ConvertHelper.ToInt(d1maxID1) == 2)
                            {
                                d1maxID1 = Convert.ToInt32(d1maxID1) + 1;
                                maxID1 = d1maxID1;//等于2时跳过加1
                            }
                            row = dt.NewRow();
                            DataRowHelper.SetDefaultValue(row);
                            row["cbilid"] = cbilid;
                            row["id1"] = d1maxID1;
                            row["cgoodsid"] = info.cgoodsid;
                            row["isortid"] = Math.Abs(Convert.ToInt32(maxID1));
                            row["cckid"] = cckid;
                            row["corgid"] = corgid;
                            row["cnote"] = info.cnote;

                            row["ref_fqty"] = phitem.fqty;
                            row["cph"] = phitem.cph;
                            row["fqty"] = phitem.fshqty;
                            row["frecrejectqty"] = phitem.fqsqty;
                            row["crejectseason"] = phitem.crejectseason;
                            row["fnormprice"] = info.fnormprice;
                            row["ref_fprice"] = info.ref_fprice;
                            row["fnormvalue"] = ConvertHelper.ToDecimal(info.fnormprice * phitem.fshqty).ToString(strvalueFormatlen);
                            row["frecrejectvalue"] = ConvertHelper.ToDecimal(info.fnormprice * phitem.fqsqty).ToString(strvalueFormatlen);

                            row["fdiscrate"] = info.fdiscrate;
                            row["fdiscvalue"] = ConvertHelper.ToDecimal(info.fnormprice * phitem.fshqty - info.fprice * phitem.fshqty).ToString(strvalueFormatlen);
                            row["fprice"] = info.fprice;
                            row["fvalue"] = ConvertHelper.ToDecimal(info.fprice * phitem.fshqty).ToString(strvalueFormatlen);
                            row["ftaxrate"] = info.ftaxrate;
                            row["ftaxvalue"] = ConvertHelper.ToDecimal(info.fnormprice * phitem.fshqty - info.fnotaxprice * phitem.fshqty).ToString(strvalueFormatlen);
                            row["fnotaxprice"] = info.fnotaxprice;
                            row["fnotaxvalue"] = ConvertHelper.ToDecimal(info.fnotaxprice * phitem.fshqty).ToString(strvalueFormatlen);
                            row["finvprice"] = info.finvprice;
                            row["finvvalue"] = ConvertHelper.ToDecimal(info.finvprice * phitem.fshqty).ToString(strvalueFormatlen);

                            row["cbz"] = info.cbz;
                            row["orig_ctabname"] = info.orig_ctabname;
                            row["orig_cbiltype"] = info.orig_cbiltype;
                            row["orig_cbilid"] = info.orig_cbilid;
                            row["orig_iid"] = info.orig_iid;
                            row["ref_cbilid"] = info.ref_cbilid;
                            row["ref_cbiltype"] = info.ref_cbiltype;
                            row["ref_ctabname"] = info.ref_ctabname;
                            row["ref_iid"] = info.ref_iid;
                            if (row.Table.Columns.Contains("csalerid") && bEnableBodySalerid && refdt != null && refdt.Rows.Count > 0)
                            {
                                var findrefRows = refdt.Select("cbilid='" + info.ref_cbilid + "' and id1=" + ConvertHelper.ToInt(info.ref_iid) + "").FirstOrDefault();
                                if (findrefRows != null)
                                {
                                    row["csalerid"] = findrefRows["csalerid"];
                                }
                            }
                            if (IsEnableSFDARenewal)
                            {
                                row["cphnote2"] = phitem.cphnote2;
                            }
                            if (!string.IsNullOrWhiteSpace(phitem.dmadedate))
                            {
                                row["dmadedate"] = Convert.ToDateTime(phitem.dmadedate);
                            }
                            else
                            {
                                row["dmadedate"] = DBNull.Value;
                            }
                            if (!string.IsNullOrWhiteSpace(phitem.dexpdate))
                            {
                                row["dexpdate"] = Convert.ToDateTime(phitem.dexpdate);
                            }
                            else
                            {
                                row["dexpdate"] = DBNull.Value;
                            }
                            row["dmjdate"] = DBNull.Value;
                            row["dmjexpdate"] = DBNull.Value;
                            row["cphcmaidcode"] = info.cphcmaidcode;
                            if (info.isgspcold == 1)
                            {
                                row["csendadd"] = info.csendadd;
                                if (!string.IsNullOrWhiteSpace(info.dsendstart))
                                {
                                    row["dsendstart"] = info.dsendstart;
                                }
                                else
                                {
                                    row["dsendstart"] = DBNull.Value;
                                }
                                if (!string.IsNullOrWhiteSpace(info.drectime))
                                {
                                    row["drectime"] = info.dsendstart;
                                }
                                else
                                {
                                    row["drectime"] = DBNull.Value;
                                }
                                row["fhours"] = info.fhours;
                                row["ctep"] = info.ctep;
                                row["csendtype"] = info.csendtype;
                                row["ctepctl"] = info.ctepctl;
                                row["ctepnote"] = info.ctepnote;
                                row["ctepsta"] = info.ctepsta;
                                row["ccar"] = info.ccar;
                                row["ccarno"] = info.ccarno;
                                row["csender"] = info.csender;
                                row["ctranser"] = info.ctranser;
                                row["ctransport"] = info.ctransport;
                                row["cdap"] = info.cdap;

                            }
                            if (row.Table.Columns.Contains("ctracecode"))//2024-07-25
                            {
                                if (request.LRInfoTrace12List != null && request.LRInfoTrace12List.Count > 0)
                                {
                                    //var find12List = request.LRInfoTrace12List.Where(a => a.id1 == ConvertHelper.ToInt(d1maxID1)).ToList();2024-11-27 修改
                                    var find12List = request.LRInfoTrace12List.Where(a => a.cgoodsid == row["cgoodsid"].ToString() && a.cph == row["cph"].ToString() && a.id1 == info.id1).ToList();
                                    if (find12List != null && find12List.Count > 0)
                                    {
                                        var ctracecodes = find12List.GroupBy(a => a.ctracename).Aggregate("", (c, r) => c + ";" + r.Key);
                                        if (!string.IsNullOrEmpty(ctracecodes) && ctracecodes.Length > 1)
                                        {
                                            row["ctracecode"] = ctracecodes.Substring(1, ctracecodes.Length - 1);
                                        }
                                    }
                                }
                            }
                            dt.Rows.Add(row);
                        }
                        #endregion
                    }

                    

                    if (bINSTRCODE)//KB023 UDI保存
                    {
                        if (IsUseZsmNewProcess)
                        {
                        	SysLog.WriteLocalLog("新版本d12数据开始录入", "PDA改造日志！！！");
                            #region 保存D12表
                            if (request.LRInfoTrace12List != null && request.LRInfoTrace12List.Count > 0)
                            {
                                if (bHaveTabled12)
                                {
                                    string cbiltype = "SH";
                                    string d1Name = "bl_shd_d1";
                                    string d12Name = "bl_rk_d12";
                                    var maxID12 = dt12.Compute("MAX(id12)", "");

                                    if (maxID12 == null || maxID12 == DBNull.Value)
                                    {
                                        maxID12 = 0;
                                    }
                                    SysLog.WriteLocalLog("新版本开始循环，maxID12的值是"+maxID12.ToString(), "PDA改造日志！！！");
                                    foreach (var item12 in request.LRInfoTrace12List)
                                    {
                                        SysLog.WriteLocalLog("新版本赋值阶段，单号是:"+cbilid+"-id1值是"+item12.id1.ToString()+"-商品编码是："+item12.cgoodsid.ToString(), "PDA改造日志！！！");
                                        var dtD1 = dt.Select("cgoodsid = '" + item12.cgoodsid + "' and cph = '" + item12.cph+"'");
                                        SysLog.WriteLocalLog("cph的值" + item12.cph, "PDA改造日志！！！");
                                        SysLog.WriteLocalLog("dtD1的值" + dtD1.Length.ToString(), "PDA改造日志！！！");

                                        if (dtD1 == null || dtD1.Length<=0)
                                        {
                                        /*
                                            response.IsError = true;
                                            response.ErrorMessage = string.Format("单据{0}不存在当前商品信息", cbilid);
                                            SysLog.WriteLocalLog(string.Format("单据{0}不存在当前商品信息", cbilid), "PDA改造日志！！！");
                                            tran.RollBack();
                                            return response;
                                            */
                                            continue;
                                        }

                                        /*重复采集*/

                                        //sql = string.Format("select tb.* from {0} as tb (NOLOCK) WHERE cbilid = @cbilid AND  cgoodsid = @cgoodsid AND id1 = @id1 ", d12Name);
                                        //var dtD12 = this.DBAccess.GetDataTable(sql, this.DBAccess.CreateDbParameter("@cbilid", cbilid)
                                        //    , this.DBAccess.CreateDbParameter("@cgoodsid", cgoodsid), this.DBAccess.CreateDbParameter("@id1", id1));
                                        //var sql = string.Format("select tb.* from {0} as tb (NOLOCK) WHERE cbilid = @cbilid AND  cgoodsid = @cgoodsid   ", d12Name);
                                        //var dtD12 = this.DBAccess.GetDataTable(sql,tran, this.DBAccess.CreateDbParameter("@cbilid", cbilid)
                                        //    , this.DBAccess.CreateDbParameter("@cgoodsid", item12.cgoodsid));
                                        /*
                                        if (dtD12.Select(string.Format("ctracename='{0}'", item12.ctracename)).Any())
                                        {
                                            response.IsError = true;
                                            SysLog.WriteLocalLog(string.Format("此码【{0}】已采集，不需要重复采集。", item12.ctracename), "PDA改造日志！！！");
                                            response.ErrorMessage = string.Format("此码【{0}】已采集，不需要重复采集。", item12.ctracename);
                                            tran.RollBack();
                                            return response;
                                        }
                                        */

                                        //var d12 = this.DBAccess.GetDataTable(string.Format("select * from {0} where cbilid =@cbilid", d12Name)
                                        //     , this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                                        //d12.TableName = d12Name;
                                        //var maxID1 = d12.Compute("MAX(id12)", "");
                                        //if (maxID1 == null || maxID1 == DBNull.Value)
                                        //{
                                        //    maxID1 = 0;
                                        //}
                                        
                                        maxID12 = Convert.ToInt32(maxID12) + 1;
                                        SysLog.WriteLocalLog("新版本赋值阶段，maxID12的值是"+maxID12.ToString()+"-单号是:"+cbilid+"-id1值是"+item12.id1.ToString()+"-商品编码是："+item12.cgoodsid.ToString(), "PDA改造日志！！！");
                                        
                                        var newRow = dt12.NewRow();

                                        //20250528 CJJ PDA采码改造 追溯码数据不重复添加
                                        if (dt12 != null && dt12.Select().Length > 0 && dt12.Select(string.Format("ctracename='{0}' AND cdatatype = 'ZSM'", item12.ctracename)).Any())
                                        {
                                            var originDataRow = dt12.Select(string.Format("ctracename='{0}'", item12.ctracename)).FirstOrDefault();
                                            if (originDataRow != null)
                                            {
                                                DataRowHelper.SetRowValue(originDataRow, newRow);
                                            }
                                            SysLog.WriteLocalLog("旧数据重新添加到dt12,Begin", "PDA改造日志！！！");
                                            dt12.Rows.Add(newRow);
                                            SysLog.WriteLocalLog("旧数据重新添加到dt12,End", "PDA改造日志！！！");
                                            continue;
                                        }

                                        DataRowHelper.SetRowValue(dtD1[0], newRow);
                                        newRow["id12"] = maxID12;
                                        newRow["cbiltype"] = cbiltype;
                                        newRow["ctracename"] = item12.ctracename;
                                        newRow["iscaner"] = request.LoginState.ID;
                                        newRow["cirter"] = request.EmpCode;
                                        newRow["clastupder"] = request.EmpCode;
                                        newRow["dscandate"] = DateTime.Now;
                                        newRow["dirtdate"] = DateTime.Now;
                                        newRow["cdatatype"] = "UDI";
                                        newRow["dlastupdtime"] = DateTime.Now;
                                        newRow["itraceid"] = 0;
                                        newRow["ilevel"] = 0;
                                        newRow["fzsmqty"] = item12.fzsmqty;
                                        newRow["fbaseqty"] = item12.fzsmqty;
                                        newRow["iqualifiedflag"] = 1;

                                        SysLog.WriteLocalLog("新版本赋值阶段，赋值给d12表时的值是" + newRow["id1"].ToString(), "PDA改造日志！！！");
                                        
                                        newRow["cph"] = item12.cph;
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

                                        //补码标识
                                        newRow["ireplenishflag"] = 0;


                                        if (dt12.Columns.Contains("cparenttrace"))
                                        {
                                            newRow["cparenttrace"] = item12.cparenttrace;
                                        }

                                        if (dt12.Columns.Contains("cdicode"))
                                        {
                                            newRow["cdicode"] = item12.cdicode;
                                        }


										SysLog.WriteLocalLog("新版本将新行添加到dt12,Begin", "PDA改造日志！！！");
                                        dt12.Rows.Add(newRow);
                                        SysLog.WriteLocalLog("新版本将新行添加到dt12,End", "PDA改造日志！！！");
                                        //var tran = this.DBAccess.CreateTransaction();
                                    }
                                    SysLog.WriteLocalLog("新版本循环结束dt12行数："+dt12.Select().Length.ToString(), "PDA改造日志！！！");
                                    //this.DBAccess.Save(dt12, tran);
                                }
                            }
                            #endregion 
                        }
                        else
                        {
							SysLog.WriteLocalLog("旧版本d12数据开始录入", "PDA改造日志！！！");
                            #region 保存D12表
                            if (request.LRInfoTrace12List != null && request.LRInfoTrace12List.Count > 0)
                            {
                                if (bHaveTabled12)
                                {
                                    var maxID12 = dt12.Compute("MAX(id12)", "");

                                    if (maxID12 == null || maxID12 == DBNull.Value)
                                    {
                                        maxID12 = 0;
                                    }
                                    foreach (var item12 in request.LRInfoTrace12List)
                                    {
                                        if (dt12.Select("cgoodsid='" + item12.cgoodsid + "' and cph='" + item12.cph + "' and cnote='" + item12.cbilid + "' and ctracename='" + item12.ctracename + "' and cparenttrace='" + item12.cparenttrace + "'").Any())
                                        {
                                            continue;
                                        }
                                        maxID12 = Convert.ToInt32(maxID12) + 1;
                                        DataRow newRow = null;
                                        newRow = dt12.NewRow();
                                        DataRowHelper.SetDefaultValue(newRow);
                                        newRow["cbilid"] = cbilid;
                                        newRow["cbiltype"] = "SH";
                                        var item1 = dt.Select(" cgoodsid = '" + item12.cgoodsid + "' and cph = '" + item12.cph + "' and ref_cbilid = '" + ConvertHelper.ToString(item12.cnote) + "' and ref_iid =" + item12.id1).FirstOrDefault();
                                        if (iSamePHProcess > 1)
                                        {
                                            if (!string.IsNullOrEmpty(item12.dmadedate) && !string.IsNullOrEmpty(item12.dexpdate))
                                            {
                                                item1 = dt.Select(" cgoodsid = '" + item12.cgoodsid + "' and cph = '" + item12.cph + "' and dmadedate='" + item12.dmadedate + " 00:00:00.000' and dexpdate='" + item12.dexpdate + " 00:00:00.000' and ref_cbilid = '" + ConvertHelper.ToString(item12.cnote) + "' and ref_iid =" + item12.id1).FirstOrDefault();
                                            }
                                            else if (string.IsNullOrEmpty(item12.dmadedate) && !string.IsNullOrEmpty(item12.dexpdate))
                                            {
                                                item1 = dt.Select(" cgoodsid = '" + item12.cgoodsid + "' and cph = '" + item12.cph + "' and ISNULL(dmadedate,'')='' and dexpdate='" + item12.dexpdate + " 00:00:00.000' and ref_cbilid = '" + ConvertHelper.ToString(item12.cnote) + "' and ref_iid =" + item12.id1).FirstOrDefault();
                                            }
                                            else if (!string.IsNullOrEmpty(item12.dmadedate) && string.IsNullOrEmpty(item12.dexpdate))
                                            {
                                                item1 = dt.Select(" cgoodsid = '" + item12.cgoodsid + "' and cph = '" + item12.cph + "' and dmadedate='" + item12.dmadedate + " 00:00:00.000' and ISNULL(dexpdate,'')='' and ref_cbilid = '" + ConvertHelper.ToString(item12.cnote) + "' and ref_iid =" + item12.id1).FirstOrDefault();
                                            }
                                        }
                                        if (item1 != null)
                                        {
                                            newRow["id1"] = ConvertHelper.ToInt(item1["id1"]);
                                        }
                                        else
                                        {
                                            newRow["id1"] = item12.id1;
                                        }
                                        newRow["id12"] = maxID12;
                                        newRow["cgoodsid"] = item12.cgoodsid;
                                        newRow["cph"] = item12.cph;
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
                                        newRow["cnote"] = item12.cnote;
                                        dt12.Rows.Add(newRow);

                                    }
                                }
                            }
                            #endregion
                            #region 保存D13表
                            if (request.LRInfoTrace13List != null && request.LRInfoTrace13List.Count > 0)
                            {
                                if (bHaveTabled13)
                                {
                                    var maxID13 = dt13.Compute("MAX(id13)", "");

                                    if (maxID13 == null || maxID13 == DBNull.Value)
                                    {
                                        maxID13 = 0;
                                    }
                                    foreach (var item13 in request.LRInfoTrace13List)
                                    {
                                        if (dt13.Select("cgoodsid='" + item13.cgoodsid + "' and cph='" + item13.cph + "' and cnote='" + item13.cbilid + "' and ctracename='" + item13.ctracename + "'").Any())
                                        {
                                            continue;
                                        }
                                        maxID13 = Convert.ToInt32(maxID13) + 1;
                                        DataRow newRow = null;
                                        newRow = dt13.NewRow();
                                        DataRowHelper.SetDefaultValue(newRow);
                                        var item1 = dt.Select(" cgoodsid = '" + item13.cgoodsid + "' and cph = '" + item13.cph + "' and ref_cbilid = '" + ConvertHelper.ToString(item13.cnote) + "' and ref_iid =" + item13.id1).FirstOrDefault();
                                        if (iSamePHProcess > 1)
                                        {
                                            if (!string.IsNullOrEmpty(item13.dmadedate) && !string.IsNullOrEmpty(item13.dexpdate))
                                            {
                                                item1 = dt.Select(" cgoodsid = '" + item13.cgoodsid + "' and cph = '" + item13.cph + "' and dmadedate='" + item13.dmadedate + " 00:00:00.000' and dexpdate='" + item13.dexpdate + " 00:00:00.000' and ref_cbilid = '" + ConvertHelper.ToString(item13.cnote) + "' and ref_iid =" + item13.id1).FirstOrDefault();
                                            }
                                            else if (string.IsNullOrEmpty(item13.dmadedate) && !string.IsNullOrEmpty(item13.dexpdate))
                                            {
                                                item1 = dt.Select(" cgoodsid = '" + item13.cgoodsid + "' and cph = '" + item13.cph + "' and ISNULL(dmadedate,'')='' and dexpdate='" + item13.dexpdate + " 00:00:00.000' and ref_cbilid = '" + ConvertHelper.ToString(item13.cnote) + "' and ref_iid =" + item13.id1).FirstOrDefault();
                                            }
                                            else if (!string.IsNullOrEmpty(item13.dmadedate) && string.IsNullOrEmpty(item13.dexpdate))
                                            {
                                                item1 = dt.Select(" cgoodsid = '" + item13.cgoodsid + "' and cph = '" + item13.cph + "' and dmadedate='" + item13.dmadedate + " 00:00:00.000' and ISNULL(dexpdate,'')='' and ref_cbilid = '" + ConvertHelper.ToString(item13.cnote) + "' and ref_iid =" + item13.id1).FirstOrDefault();
                                            }
                                        }
                                        if (item1 != null)
                                        {
                                            newRow["id1"] = ConvertHelper.ToInt(item1["id1"]);
                                        }
                                        else
                                        {
                                            newRow["id1"] = item13.id1;
                                        }
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
                                }
                            }
                            #endregion
                    
                        }
                    }
                    #endregion
                    #region 修改明细中冷藏记录

                    SysLog.WriteLocalLog("冷藏记录是否要录入："+IsColdGoods.ToString(), "PDA改造日志！！！");

                    if (IsColdGoods)
                    {
                        foreach (var item in dt.Select())
                        {
                            var iscold = this.DBAccess.ExecuteScalar(@"SELECT TOP 1 isgspcold FROM bf_goods(NOLOCK) WHERE cgoodsid=@cgoodsid", tran,
                       this.DBAccess.CreateDbParameter("@cgoodsid", item["cgoodsid"].ToString()));
                            if ((int)iscold == 1)
                            {
                                item["csendadd"] = request.ColdStore.csendadd;
                                if (!string.IsNullOrWhiteSpace(request.ColdStore.dsendstart))
                                {
                                    item["dsendstart"] = request.ColdStore.dsendstart;
                                }
                                else
                                {
                                    item["dsendstart"] = DBNull.Value;
                                }
                                if (!string.IsNullOrWhiteSpace(request.ColdStore.drectime))
                                {
                                    item["drectime"] = request.ColdStore.drectime;
                                }
                                else
                                {
                                    item["drectime"] = DBNull.Value;
                                }
                                //item["dsendstart"] = request.ColdStore.dsendstart;
                                //item["drectime"] = request.ColdStore.drectime;
                                item["fhours"] = request.ColdStore.fhours;
                                item["ctep"] = request.ColdStore.ctep;
                                item["csendtype"] = request.ColdStore.csendtype;
                                item["ctepctl"] = request.ColdStore.ctepctl;
                                item["ctepnote"] = request.ColdStore.ctepnote;
                                item["ctepsta"] = request.ColdStore.ctepsta;
                                item["ccar"] = request.ColdStore.ccar;
                                item["ccarno"] = request.ColdStore.ccarno;
                                item["csender"] = request.ColdStore.csender;
                                item["ctranser"] = request.ColdStore.ctranser;
                                item["ctransport"] = request.ColdStore.ctransport;
                                item["cdap"] = request.ColdStore.cdap;
                                item["cstep"] = request.ColdStore.cstep;
                                item["csdap"] = request.ColdStore.csdap;
                            }
                        }
                    }

                    #endregion

                    // this.DBAccess.Save(dt, tran);
                    SysLog.WriteLocalLog("dt12add", "PDA改造日志！！！");
                    if (dt12 != null && dt12.Rows.Count > 0)
                    {
                        ds.Tables.Add(dt12);
                        SysLog.WriteLocalLog("dt12add-ing", "PDA改造日志！！！");
                        // this.DBAccess.Save(dt12, tran);
                    }

                    SysLog.WriteLocalLog("dt13add", "PDA改造日志！！！");
                    if (dt13 != null && dt13.Rows.Count > 0)
                    {
                        ds.Tables.Add(dt13);
                        //this.DBAccess.Save(dt13, tran);
                    }

                    var head = this.DBAccess.GetDataTable("select * from bl_shd (nolock) where cbilid=@cbilid", tran, this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                    head.TableName = "bl_shd";
                    SysLog.WriteLocalLog("cbilid是:" + cbilid, "PDA改造日志！！！");
                    SysLog.WriteLocalLog("SUMBILLHEAD", "PDA改造日志！！！");
                    SysLog.WriteLocalLog("head有值吗？"+head.Select().Length.ToString(), "PDA改造日志！！！");
                    SumBillHead(head, dt);

                    ds.Tables.Add(head);

                    //this.DBAccess.Save(head, tran);     //合并表头数据金额信息
                    SysLog.WriteLocalLog("ds的表数量"+ds.Tables.Count.ToString(), "PDA改造日志！！！");
                    SysLog.WriteLocalLog("d12表的行数量" + dt12.Select().Length.ToString(), "PDA改造日志！！！");
                    SysLog.WriteLocalLog("来保存来了！", "PDA改造日志！！！");
                    if (!this.DBAccess.Save(ds, tran))
                    {
                        tran.RollBack();
                    }
                    else
                    {
                        #region 调用工具栏配置的存储过程
                        var toopBarProcResponse = this.ExecuteOther(
                                new BillToolBarProcActionRequest()
                                {
                                    ModuleID = "DMSM01001210",
                                    BillNO = cbilid,
                                    BillType = "SH",
                                    UserID = request.EmpCode,
                                    ActionType = "save", //function.ActionType,
                                    Action = "save-ok", // overwriteAction != null ? overwriteAction : function.Action,
                                    Note = "",
                                    OrgID = corgid.ToString(),
                                    Cirter = request.EmpCode,
                                    BillTypeName = "", // xop.WindowsTitle,
                                    transaction = tran,
                                    async = false,
                                    MenuCode = "",//2018-7-30 ZGH
                                    MenuName = "",
                                    InvokeType = 1
                                }
                                );

                        if (toopBarProcResponse.IsError || !toopBarProcResponse.Result.Success)
                        {
                            tran.RollBack();
                        }
                        else
                        {
                            toopBarProcResponse = this.ExecuteOther(
                                new BillToolBarProcActionRequest()
                                {
                                    ModuleID = "DMSM01001210",
                                    BillNO = cbilid,
                                    BillType = "SH",
                                    UserID = request.EmpCode,
                                    ActionType = "save", //function.ActionType,
                                    Action = "save-ok", // overwriteAction != null ? overwriteAction : function.Action,
                                    Note = "",
                                    OrgID = corgid.ToString(),
                                    Cirter = request.EmpCode,
                                    BillTypeName = "", // xop.WindowsTitle,
                                    transaction = tran,
                                    async = false,
                                    MenuCode = "",//2018-7-30 ZGH
                                    MenuName = "",
                                    InvokeType = 2
                                }
                                );
                            if (toopBarProcResponse.IsError || !toopBarProcResponse.Result.Success)
                            {
                                tran.RollBack();
                            }
                            else
                            {
                                tran.Commit();
                            }
                        }
                        #endregion
                    }

                    #region BUG#60563开启新流程则需要判断

                    if (IsUseZsmNewProcess)
                    {
                        var checkFinishSql = @"
SELECT TOP 1 1 FROM dbo.bl_shd_d1(NOLOCK) shd1 
INNER JOIN dbo.bf_goods(NOLOCK) g ON g.cgoodsid = shd1.cgoodsid
WHERE 
(NOT EXISTS(
SELECT TOP 1 1 FROM
(SELECT SUM(d12.fzsmqty) AS fzsmqty,d12.cgoodsid,d12.cph FROM dbo.fun_getctracenamebycbilid(shd1.cbilid) d12 GROUP BY d12.cgoodsid,d12.cph)
d WHERE d.cgoodsid = shd1.cgoodsid AND d.cph = shd1.cph
)
OR
EXISTS(
SELECT TOP 1 1 FROM
(SELECT SUM(d12.fzsmqty) AS fzsmqty,d12.cgoodsid,d12.cph FROM dbo.fun_getctracenamebycbilid(shd1.cbilid) d12 GROUP BY d12.cgoodsid,d12.cph)
c WHERE c.fzsmqty<shd1.fqty AND c.cgoodsid = shd1.cgoodsid AND c.cph = shd1.cph))
AND shd1.cbilid = @cbilid
AND (G.izsmflag= 1 OR G.iudiflag = 1 OR	 G.ifcflag = 1)";

                        var checkFinish = ConvertHelper.ToInt(this.DBAccess.ExecuteScalar(checkFinishSql, this.DBAccess.CreateDbParameter("@cbilid", cbilid)))==1;
                        if(checkFinish){
                        response.IsError = checkFinish;
                        response.ErrorMessage = "isNotFinishScanYet";//这里用了个比较奇葩的回参方法，属于没有办法的办法
                        }
                    }

                    #endregion
                }
                catch (Exception ex)
                {
                    response.IsError = true;
                    response.ErrorMessage = ex.Message;
                    tran.RollBack();
                }
                
                #endregion
            }
            else if (request.OPType == 8)
            {
                #region 收货单已收记录
                if (request.cbilid == null || request.cbilid == "")
                {
                    response.IsError = true;
                    response.ErrorMessage = "参数不完整！";
                    return response;
                }

                //V1.2
                string sql = @"
SELECT bl_shd_d1.cgoodsid, bl_shd_d1.cph, bl_shd_d1.ref_cbilid As cbilid, bl_shd_d1.id1 As id1, bl_shd_d1.fqty, bl_shd_d1.fnormprice, bl_shd_d1.fnormvalue, bl_shd_d1.cnote
, bl_shd_d1.frecrejectqty, bl_shd_d1.frecrejectvalue, bl_shd_d1.crejectseason, bl_shd_d1.crecer, bl_shd_d1.ref_fqty
,bl_shd_d1.dmadedate,bl_shd_d1.dexpdate,bl_shd_d1.cphnote,bl_shd_d1.cmjph,bl_shd_d1.dmjdate,bl_shd_d1.dmjexpdate,bl_shd_d1.cmjphnote
,v_bf_goods0.cgoodsname as cgoodsid_v_cgoodsname,v_bf_goods0.ccommonname as cgoodsid_v_ccommonname,v_bf_goods0.cpkname as cgoodsid_v_cpkname,v_bf_goods0.cprodaddress as cgoodsid_v_cprodaddress,v_bf_goods0.cfactoryname as cgoodsid_v_cfactoryname,v_bf_goods0.cfileno as cgoodsid_v_cfileno,v_bf_goods0.cunit as cgoodsid_v_cunit,
v_bf_goods0.iphflag as cgoodsid_v_iphflag,v_bf_goods0.imjphflag as cgoodsid_v_imjphflag,v_bf_goods0.izsmflag as cgoodsid_v_izsmflag,v_bf_goods0.cbarcode
,bl_shd_d1.cbz, bl_shd_d1.orig_ctabname, bl_shd_d1.orig_cbiltype, bl_shd_d1.orig_cbilid, bl_shd_d1.orig_iid, bl_shd_d1.ref_cbilid, bl_shd_d1.ref_cbiltype, bl_shd_d1.ref_ctabname, bl_shd_d1.ref_iid
,bl_shd_d1.csendadd, bl_shd_d1.dsendstart, bl_shd_d1.drectime, bl_shd_d1.fhours, bl_shd_d1.ctep
,bl_shd_d1.csendtype, bl_shd_d1.ctepctl, bl_shd_d1.ctepnote, bl_shd_d1.ctepsta, bl_shd_d1.ccar
,bl_shd_d1.ccarno, bl_shd_d1.csender, bl_shd_d1.ctranser, bl_shd_d1.ctransport, bl_shd_d1.cdap
,bl_shd_d1.fdiscrate,bl_shd_d1.fdiscvalue, bl_shd_d1.fprice,bl_shd_d1.fvalue,bl_shd_d1.ftaxrate,bl_shd_d1.ftaxvalue,bl_shd_d1.fnotaxprice,bl_shd_d1.fnotaxvalue,bl_shd_d1.finvprice,bl_shd_d1.finvvalue,bl_shd_d1.cphcmaidcode
,v_bf_goods0.ccertificateno
FROM bl_shd_d1 WITH(NOLOCK) LEFT JOIN bf_goods As v_bf_goods0 WITH(NOLOCK) ON bl_shd_d1.cgoodsid = v_bf_goods0.cgoodsid
WHERE bl_shd_d1.cbilid =@cbilid
ORDER BY bl_shd_d1.cgoodsid, bl_shd_d1.id1
";
                var dt = this.DBAccess.GetDataTable(sql, this.DBAccess.CreateDbParameter("@cbilid", request.cbilid));
                response.SHGoodsList = new List<SHGoods>();
                foreach (var item in dt.Select())
                {
                    var findRow = response.SHGoodsList.Where(p => p.cgoodsid == item["cgoodsid"].ToString() && p.ref_cbilid == item["ref_cbilid"].ToString()
                                        && p.orig_iid == item["orig_iid"].ToString() && p.cph == item["cph"].ToString()).FirstOrDefault();
                    if (findRow == null)
                    {
                        var info = new SHGoods()
                        {
                            cgoodsid = item["cgoodsid"].ToString(),
                            cgoodsname = item["cgoodsid_v_cgoodsname"].ToString(),
                            cbarcode = item["cbarcode"].ToString(),
                            cprodaddress = item["cgoodsid_v_cprodaddress"].ToString(),
                            cfileno = item["cgoodsid_v_cfileno"].ToString(),
                            cpkname = item["cgoodsid_v_cpkname"].ToString(),
                            cfactoryname = item["cgoodsid_v_cfactoryname"].ToString(),
                            cunit = item["cgoodsid_v_cunit"].ToString(),
                            iphflag = item["cgoodsid_v_iphflag"].ToInt(),
                            cph = item["cph"].ToString(),
                            dmadedate = item["dmadedate"].ToString(),
                            dexpdate = item["dexpdate"].ToString(),
                            cbilid = item["cbilid"].ToString(),
                            id1 = item["id1"].ToInt(),
                            fqty = Convert.ToDecimal(item["fqty"] == DBNull.Value ? 0 : item["fqty"]),
                            fnormprice = Convert.ToDecimal(item["fnormprice"] == DBNull.Value ? 0 : item["fnormprice"]),
                            fnormvalue = Convert.ToDecimal(item["fnormvalue"] == DBNull.Value ? 0 : item["fnormvalue"]),
                            fdiscrate = Convert.ToDecimal(item["fdiscrate"] == DBNull.Value ? 0 : item["fdiscrate"]),
                            fdiscvalue = Convert.ToDecimal(item["fdiscvalue"] == DBNull.Value ? 0 : item["fdiscvalue"]),
                            fprice = Convert.ToDecimal(item["fprice"] == DBNull.Value ? 0 : item["fprice"]),
                            fvalue = Convert.ToDecimal(item["fvalue"] == DBNull.Value ? 0 : item["fvalue"]),
                            ftaxrate = Convert.ToDecimal(item["ftaxrate"] == DBNull.Value ? 0 : item["ftaxrate"]),
                            ftaxvalue = Convert.ToDecimal(item["ftaxvalue"] == DBNull.Value ? 0 : item["ftaxvalue"]),
                            fnotaxprice = Convert.ToDecimal(item["fnotaxprice"] == DBNull.Value ? 0 : item["fnotaxprice"]),
                            fnotaxvalue = Convert.ToDecimal(item["fnotaxvalue"] == DBNull.Value ? 0 : item["fnotaxvalue"]),
                            finvprice = Convert.ToDecimal(item["finvprice"] == DBNull.Value ? 0 : item["finvprice"]),
                            finvvalue = Convert.ToDecimal(item["finvvalue"] == DBNull.Value ? 0 : item["finvvalue"]),

                            cnote = item["cnote"].ToString(),
                            frecrejectqty = Convert.ToDecimal(item["frecrejectqty"] == DBNull.Value ? 0 : item["frecrejectqty"]),
                            frecrejectvalue = Convert.ToDecimal(item["frecrejectvalue"] == DBNull.Value ? 0 : item["frecrejectvalue"]),
                            ref_fqty = Convert.ToDecimal(item["ref_fqty"] == DBNull.Value ? 0 : item["ref_fqty"]),
                            crejectseason = item["crejectseason"].ToString(),
                            cbz = item["cbz"].ToString(),
                            orig_ctabname = item["orig_ctabname"].ToString(),
                            orig_cbiltype = item["orig_cbiltype"].ToString(),
                            orig_cbilid = item["orig_cbilid"].ToString(),
                            orig_iid = item["orig_iid"].ToString(),
                            ref_cbilid = item["ref_cbilid"].ToString(),
                            ref_cbiltype = item["ref_cbiltype"].ToString(),
                            ref_ctabname = item["ref_ctabname"].ToString(),
                            ref_iid = item["ref_iid"].ToString(),
                            csendadd = item["csendadd"].ToString(),
                            dsendstart = item["dsendstart"].ToString(),
                            drectime = item["drectime"].ToString(),
                            fhours = Convert.ToDecimal(item["fhours"] == DBNull.Value ? 0 : item["fhours"]),
                            ctep = item["ctep"].ToString(),
                            csendtype = item["csendtype"].ToString(),
                            ctepctl = item["ctepctl"].ToString(),
                            ctepnote = item["ctepnote"].ToString(),
                            ctepsta = item["ctepsta"].ToString(),
                            ccar = item["ccar"].ToString(),
                            ccarno = item["ccarno"].ToString(),
                            csender = item["csender"].ToString(),
                            ctranser = item["ctranser"].ToString(),
                            ctransport = item["ctransport"].ToString(),
                            cdap = item["cdap"].ToString(),
                            cphcmaidcode = item["cphcmaidcode"].ToString(),
                            ccertificateno = ConvertHelper.ToString(item["ccertificateno"]),//V1.2
                            PHList = new List<SHPHInfo>()
                        };
                        var ph = new SHPHInfo()
                        {
                            cph = info.cph,
                            cgoodsid = info.cgoodsid,
                            cgoodsname = info.cgoodsname,
                            cmjph = "",
                            dmadedate = info.dmadedate,
                            dexpdate = info.dexpdate,
                            fqty = info.ref_fqty,
                            fshqty = info.fqty,
                            fqsqty = info.frecrejectqty,
                            iphflag = info.iphflag,
                            crejectseason = item["crejectseason"].ToString(),
                            iterm = info.iterm,
                        };
                        info.PHList.Add(ph);
                        response.SHGoodsList.Add(info);
                    }
                    else
                    {
                        var ph = new SHPHInfo()
                        {
                            cph = item["cph"].ToString(),
                            cgoodsid = item["cgoodsid"].ToString(),
                            cgoodsname = item["cgoodsid_v_cgoodsname"].ToString(),
                            cmjph = "",
                            dmadedate = item["dmadedate"].ToString(),
                            dexpdate = item["dexpdate"].ToString(),
                            fqty = Convert.ToDecimal(item["ref_fqty"] == DBNull.Value ? 0 : item["ref_fqty"]),
                            fshqty = Convert.ToDecimal(item["fqty"] == DBNull.Value ? 0 : item["fqty"]),
                            fqsqty = Convert.ToDecimal(item["frecrejectqty"] == DBNull.Value ? 0 : item["frecrejectqty"]),
                            crejectseason = item["crejectseason"].ToString(),
                            iphflag = item["cgoodsid_v_iphflag"].ToInt(),
                        };
                        findRow.PHList.Add(ph);
                    }
                }
                #region UDI明细
                if (this.DBCache.Get("sy_config").Select("cparmname='INSTRCODE' and cparmvalue='1'").Length > 0)//启用了UDI参数
                {
                    if (this.DBCache.Get("sy_dt_tablelist").Select("tablename='bl_shd_d12'").Length > 0)
                    {
                        response.LRInfoTrace12List = new List<GoodsTrace12>();

                        if (ConvertHelper.ToInt(this.DBAccess.ExecuteScalar("SELECT TOP 1 cparmvalue from sy_config where cparmname='UseZsmNewProcess'"))==1)
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

SELECT DISTINCT * FROM #temp_trace_d12_data";
                        }
                        else
                        {
                            sql = "select * from bl_shd_d12 where cbilid=@cbilid";
                        }

                        
                        dt = this.DBAccess.GetDataTable(sql, this.DBAccess.CreateDbParameter("@cbilid", request.cbilid));
                        foreach (var item12 in dt.Select())
                        {
                            //V1.3
                            //var findRow12 = response.LRInfoTrace12List.Where(p => p.cgoodsid == item12["cgoodsid"].ToString() && p.id1 == ConvertHelper.ToInt(item12["id1"])
                            //                    && p.ctracename == item12["ctracename"].ToString() && p.cph == item12["cph"].ToString()).FirstOrDefault();
                            //if (findRow12 == null)
                            //{
                                var item1 = response.SHGoodsList.Where(a => a.cgoodsid == item12["cgoodsid"].ToString() && a.cph == item12["cph"].ToString() && (ConvertHelper.ToString(item12["cnote"]) == ConvertHelper.ToString(a.ref_cbilid)) && ConvertHelper.ToInt(a.id1) == ConvertHelper.ToInt(item12["id1"])).FirstOrDefault();
                                var GoodsTrace12 = new GoodsTrace12()
                                {

                                    //var item1 = phInfo.Where(a => a.cgoodsid == item12.cgoodsid && a.cph == item12.cph && (ConvertHelper.ToString(item12.cbilid) == ConvertHelper.ToString(info.ref_cbilid)) && ConvertHelper.ToInt(info.ref_iid) == ConvertHelper.ToInt(item12.id1)).FirstOrDefault();
                                    cbilid = (item1 != null) ? item1.ref_cbilid : ConvertHelper.ToString(item12["cbilid"]),
                                    id1 = (item1 != null) ? ConvertHelper.ToInt(item1.ref_iid) : ConvertHelper.ToInt(item12["id1"]),
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
                                    fzsmqty = ConvertHelper.ToDecimal(item12["fzsmqty"]),
                                    cnote = ConvertHelper.ToString(item12["cnote"])
                                };
                                response.LRInfoTrace12List.Add(GoodsTrace12);
                            //}
                        }
                    }
                    if (this.DBCache.Get("sy_dt_tablelist").Select("tablename='bl_shd_d13'").Length > 0)
                    {
                        response.LRInfoTrace13List = new List<GoodsTrace13>();
                        sql = "select * from bl_shd_d13 where cbilid=@cbilid";
                        dt = this.DBAccess.GetDataTable(sql, this.DBAccess.CreateDbParameter("@cbilid", request.cbilid));
                        foreach (var item13 in dt.Select())
                        {
                            //var findRow12 = response.LRInfoTrace12List.Where(p => p.cgoodsid == item13["cgoodsid"].ToString() && p.id1 == ConvertHelper.ToInt(item13["id1"])
                            //                    && p.ctracename == item13["ctracename"].ToString() && p.cph == item13["cph"].ToString()).FirstOrDefault();
                            //if (findRow12 == null)
                            //{
                                var item1 = response.SHGoodsList.Where(a => a.cgoodsid == item13["cgoodsid"].ToString() && a.cph == item13["cph"].ToString() && (ConvertHelper.ToString(item13["cnote"]) == ConvertHelper.ToString(a.ref_cbilid)) && ConvertHelper.ToInt(a.id1) == ConvertHelper.ToInt(item13["id1"])).FirstOrDefault();
                                var GoodsTrace13 = new GoodsTrace13()
                                {
                                    cbilid = (item1 != null) ? item1.ref_cbilid : ConvertHelper.ToString(item13["cbilid"]),
                                    id1 = (item1 != null) ? ConvertHelper.ToInt(item1.ref_iid) : ConvertHelper.ToInt(item13["id1"]),
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
                            //}
                        }
                    }
                }
                #endregion
                #endregion
            }
            else if (request.OPType == 9)
            {
                #region 提交（入账）收货单
                if (request.cbilid == null || request.cbilid == "")
                {
                    response.IsError = true;
                    response.ErrorMessage = "参数不完整！";
                    return response;
                }
                var corgid = this.DBAccess.ExecuteScalar("select top 1 corgid from dbo.bl_shd(NOLOCK) where cbilid=@cbilid ",
                    this.DBAccess.CreateDbParameter("@cbilid", request.cbilid));
                if (corgid == null)
                {
                    response.IsError = true;
                    response.ErrorMessage = "找不到此收货单，请刷新数据！";
                    return response;
                }
                var obj = this.DBAccess.ExecuteScalar("select top 1 1 from dbo.bl_shd_d1 (NOLOCK) where cbilid=@cbilid ",
                    this.DBAccess.CreateDbParameter("@cbilid", request.cbilid));
                if (obj == null)
                {
                    response.IsError = true;
                    response.ErrorMessage = "此收货单没有收货数据，不允许提交！";
                    return response;
                }
                var iflag = this.DBAccess.ExecuteScalar("select top 1 iflag from dbo.bl_shd (NOLOCK) where cbilid=@cbilid ",
                    this.DBAccess.CreateDbParameter("@cbilid", request.cbilid));
                if (iflag != null && iflag.ToInt() >= 100)
                {
                    var iflagtext = this.DBAccess.ExecuteScalar("SELECT ccodetext FROM sy_lookup (nolock) WHERE ctype='BILLFLAG' AND ccodevalue = '" + iflag.ToString() + "'");
                    response.IsError = true;
                    response.ErrorMessage = "保存失败！单据已经" + iflagtext + "不允许修改，请刷新数据。";
                    return response;
                }
                var accResponse = this.ExecuteOther(
                     new BillToolBarProcActionRequest()
                     {
                         BillNO = request.cbilid,
                         BillType = "SH",
                         UserID = "admin",
                         ActionType = "acc", //function.ActionType,
                         Action = "acc-ok", // overwriteAction != null ? overwriteAction : function.Action,
                         Note = "",
                         OrgID = corgid.ToString(),
                         Cirter = request.EmpCode,
                         BillTypeName = "", // xop.WindowsTitle,
                         transaction = null,
                         async = false,
                         MenuCode = "",//2018-7-30 ZGH
                         MenuName = "",
                         InvokeType = 0
                     }
                     );
                if (accResponse.IsError || !accResponse.Result.Success)
                {
                    response.IsError = true;
                    response.ErrorMessage = accResponse.IsError ? accResponse.ErrorMessage : accResponse.Result.Message;
                    return response;
                }
                #endregion
            }
            else if (request.OPType == 10)
            {
                #region 修改冷藏商品的冷藏记录属性
                if (request.cbilid == null || request.cbilid == "" || request.ColdStore == null
                        || request.ColdStore.csendtype == null || request.ColdStore.csendtype == string.Empty
                        || request.ColdStore.ctepctl == null || request.ColdStore.ctepctl == string.Empty
                    )
                {
                    response.IsError = true;
                    response.ErrorMessage = "参数不完整！";
                    return response;
                }

                var cbilid = this.DBAccess.ExecuteScalar(@"SELECT TOP 1 cbilid FROM bl_shd(NOLOCK) WHERE cbilid=@cbilid",
                   this.DBAccess.CreateDbParameter("@cbilid", request.cbilid));
                if (cbilid == null)
                {
                    response.IsError = true;
                    response.ErrorMessage = "收货单单据（" + request.cbilid + "）不存在！";
                    return response;
                }
                var iflag = this.DBAccess.ExecuteScalar("select top 1 iflag from dbo.bl_shd (NOLOCK) where cbilid=@cbilid ",
                    this.DBAccess.CreateDbParameter("@cbilid", request.cbilid));
                if (iflag != null && iflag.ToInt() >= 100)
                {
                    var iflagtext = this.DBAccess.ExecuteScalar("SELECT ccodetext FROM sy_lookup (nolock) WHERE ctype='BILLFLAG' AND ccodevalue = '" + iflag.ToString() + "'");
                    response.IsError = true;
                    response.ErrorMessage = "保存失败！单据已经" + iflagtext + "不允许修改，请刷新数据。";
                    return response;
                }
                string sql = @"
                UPDATE bl_shd_d1 SET csendadd = @csendadd, dsendstart = @dsendstart, drectime = @drectime, fhours = @fhours, ctep = @ctep
                , csendtype = @csendtype, ctepctl = @ctepctl, ctepnote = @ctepnote, ctepsta = @ctepsta
                , ccar = @ccar, ccarno = @ccarno, csender = @csender, ctranser = @ctranser, ctransport = @ctransport, cdap = @cdap
                , cstep = @cstep, csdap = @csdap
                FROM bl_shd_d1 LEFT JOIN bf_goods ON bl_shd_d1.cgoodsid = bf_goods.cgoodsid
                WHERE bf_goods.isgspcold <> 0 AND bl_shd_d1.cbilid = @cbilid

                ";
                this.DBAccess.ExecuteNonQuery(sql, this.DBAccess.CreateDbParameter("@cbilid", request.cbilid)
                     , this.DBAccess.CreateDbParameter("@csendadd", request.ColdStore.csendtype), this.DBAccess.CreateDbParameter("@dsendstart", request.ColdStore.dsendstart)
                     , this.DBAccess.CreateDbParameter("@drectime", request.ColdStore.drectime), this.DBAccess.CreateDbParameter("@fhours", request.ColdStore.fhours)
                     , this.DBAccess.CreateDbParameter("@ctep", request.ColdStore.ctep), this.DBAccess.CreateDbParameter("@csendtype", request.ColdStore.csendtype)
                     , this.DBAccess.CreateDbParameter("@ctepctl", request.ColdStore.ctepctl), this.DBAccess.CreateDbParameter("@ctepnote", request.ColdStore.ctepnote)
                     , this.DBAccess.CreateDbParameter("@ctepsta", request.ColdStore.ctepsta), this.DBAccess.CreateDbParameter("@ccar", request.ColdStore.ccar)
                     , this.DBAccess.CreateDbParameter("@ccarno", request.ColdStore.ccarno), this.DBAccess.CreateDbParameter("@csender", request.ColdStore.csender)
                     , this.DBAccess.CreateDbParameter("@ctranser", request.ColdStore.ctranser), this.DBAccess.CreateDbParameter("@ctransport", request.ColdStore.ctransport)
                     , this.DBAccess.CreateDbParameter("@cdap", request.ColdStore.cdap), this.DBAccess.CreateDbParameter("@cstep", request.ColdStore.cstep)
                     , this.DBAccess.CreateDbParameter("@csdap", request.ColdStore.csdap)
                     );
                //if (count == 0)
                //{
                //response.IsError = true;
                //response.ErrorMessage = "更新失败，受影响行数为0，请刷新数据！";
                //return response;
                //}
                #endregion
            }
            else if (request.OPType == 11)
            {
                #region 删除已收记录
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
                if (request.OrgID == null || request.OrgID == "")
                {
                    response.IsError = true;
                    response.ErrorMessage = "参数不完整！";
                    return response;
                }
                Transaction tran = null;
                tran = this.DBAccess.CreateTransaction();
                var cbilid = request.cbilid;
                var cckid = this.DBAccess.ExecuteScalar(@"SELECT TOP 1 cckid FROM bl_shd(NOLOCK) WHERE cbilid=@cbilid",
                  this.DBAccess.CreateDbParameter("@cbilid", cbilid));

                var corgid = this.DBAccess.ExecuteScalar(@"SELECT TOP 1 corgid FROM bl_shd(NOLOCK) WHERE cbilid=@cbilid",
                   this.DBAccess.CreateDbParameter("@cbilid", cbilid));

                var dt = this.DBAccess.GetDataTable("SELECT * FROM bl_shd_d1(nolock) where cbilid=@cbilid", this.DBAccess.CreateDbParameter("@cbilid", cbilid));
                dt.TableName = "bl_shd_d1";
                var configRows = this.DBCache.Get("sy_config").Select("cparmname in ('PRICEDECLEN','QTYDECLEN','RATEDECLEN','TAXVALUEDECLEN','VALUEDECLEN','PKQTYDECLEN','NTPRICEDECLEN','EXRATEDECLEN')");
                var valueFormatlen = configRows.FirstOrDefault(r => r["cparmname"].ToString() == "VALUEDECLEN")["cparmvalue"];
                //foreach (var info in request.LRInfoList)
                //{
                //    var phInfo = info.PHList;
                //    foreach (var phitem in phInfo)
                //    {
                //        var findRow = dt.Select(string.Format("cgoodsid='{0}' and ref_cbilid='{1}' and ref_iid='{2}' and cph='{3}' "
                //                                , info.cgoodsid, info.ref_cbilid, info.orig_iid, phitem.cph)).FirstOrDefault();
                //        if (findRow != null)
                //        {
                //            findRow.Delete();
                //        }
                //    }
                //}
                foreach (var info in dt.Select())
                {
                    var goodsInfo = request.LRInfoList.Where(p => p.cgoodsid == info["cgoodsid"].ToString()).FirstOrDefault();
                    if (goodsInfo != null)
                    {
                        var phInfo = goodsInfo.PHList.Where(p => p.cph == info["cph"].ToString()).FirstOrDefault();
                        if (phInfo == null)
                        {
                            #region PDA删除收货数据时处理
                            if (!string.IsNullOrEmpty(goodsInfo.ref_cbilid) && goodsInfo.ref_cbilid != "-" && !string.IsNullOrEmpty(goodsInfo.ref_ctabname) && goodsInfo.ref_ctabname == "bl_cgdd_d1")
                            {
                                var sql = @"update  bl_cgdd_d1 set 
	                    fturnqty= ISNULL(CASE WHEN sh.fqty+sh.frecrejectqty>bl_cgdd_d1.fqty THEN bl_cgdd_d1.fqty ELSE sh.fqty+sh.frecrejectqty END,0)- CONVERT(VARCHAR(50),@in_fqty), 
	                    frecqty= ISNULL(CASE WHEN sh.fqty>bl_cgdd_d1.fqty THEN bl_cgdd_d1.fqty ELSE sh.fqty END,0)- CONVERT(VARCHAR(50),@in_fqty) ,
	                    frecvalue= ROUND((ISNULL(CASE WHEN sh.fqty>bl_cgdd_d1.fqty THEN bl_cgdd_d1.fqty ELSE sh.fqty END,0)- CONVERT(VARCHAR(50),@in_fqty))*fprice, convert(int,@valuedeclen)),
	                    frecrejectqty=ISNULL(sh.frecrejectqty,0),
	                    frecrejectvalue= ROUND(ISNULL(sh.frecrejectqty,0)*fprice,convert(int,@valuedeclen))
                    FROM bl_cgdd_d1
                    LEFT JOIN 
                    (SELECT bl_shd_d1.ref_cbilid,bl_shd_d1.ref_iid,
	                    SUM(fqty) AS fqty,SUM(case when bl_shd.iflag<100 then frecrejectqty else 0 end)+ISNULL(MAX(js.fcancelqty),0) AS frecrejectqty
	                    FROM bl_shd(nolock) 
	                    INNER JOIN bl_shd_d1(nolock) ON bl_shd_d1.cbilid = bl_shd.cbilid
	                    LEFT JOIN (
	                        SELECT orig_cbilid,orig_iid,SUM(fcancelqty) AS fcancelqty
	                        FROM 
	                        (
		                        SELECT d1.orig_cbilid,d1.orig_iid,SUM(case when bt.iflag>=100 THEN 0 ELSE d1.fqty end) AS fcancelqty 
		                        FROM dbo.bl_jsd(nolock) bt 
		                        INNER JOIN dbo.bl_jsd_d1(nolock) d1 ON d1.cbilid = bt.cbilid and d1.ref_cbiltype='SH'
		                        WHERE EXISTS(SELECT 1 FROM bl_shd_d1(nolock) p1 INNER JOIN bl_shd p ON p.cbilid = p1.cbilid 
						                    WHERE p.iflag = 100 AND p1.orig_cbilid = d1.orig_cbilid AND p1.orig_iid = d1.orig_iid
						                    and p1.orig_cbilid =@in_refcbilid and p1.orig_iid=@in_refiid
						                    )
		                        GROUP BY d1.orig_cbilid,d1.orig_iid
		                        UNION ALL
		                        SELECT d1.orig_cbilid,d1.orig_iid,SUM(d1.fqty) AS fcancelqty 
		                        FROM dbo.bl_jsd(nolock) bt 
		                        INNER JOIN dbo.bl_jsd_d1(nolock) d1 ON d1.cbilid = bt.cbilid and d1.ref_cbiltype='SH'
		                        WHERE bt.iflag = 100 
		                        AND EXISTS(SELECT 1 FROM bl_shd_d1(nolock) p1 WHERE p1.orig_cbilid = d1.orig_cbilid AND p1.orig_iid = d1.orig_iid
						                    and p1.orig_cbilid =@in_refcbilid and p1.orig_iid=@in_refiid
		                        )
		                        GROUP BY d1.orig_cbilid,d1.orig_iid
	                        )js1 GROUP BY orig_cbilid,orig_iid
	                    ) js ON js.orig_cbilid = bl_shd_d1.ref_cbilid AND js.orig_iid = bl_shd_d1.ref_iid
	                    WHERE bl_shd.ishtype = 1 AND bl_shd.iflag <> 999
	                        AND bl_shd_d1.cbilid<>@in_cbilid 
	                        AND bl_shd_d1.ref_cbilid =@in_refcbilid and bl_shd_d1.orig_iid=@in_refiid
	                    GROUP BY bl_shd_d1.ref_cbilid,bl_shd_d1.ref_iid
                    ) sh 
                    on bl_cgdd_d1.cbilid = sh.ref_cbilid AND bl_cgdd_d1.id1 = sh.ref_iid
                    WHERE cbilid=@in_refcbilid and id1=@in_refiid";
                                this.DBAccess.ExecuteNonQuery(sql, tran
                                    , this.DBAccess.CreateDbParameter("@in_reftabname", goodsInfo.ref_ctabname)
                                    , this.DBAccess.CreateDbParameter("@in_fqty", goodsInfo.fqty)
                                    , this.DBAccess.CreateDbParameter("@valuedeclen", valueFormatlen)
                                    , this.DBAccess.CreateDbParameter("@in_refcbilid", goodsInfo.ref_cbilid)
                                    , this.DBAccess.CreateDbParameter("@in_refiid", goodsInfo.ref_iid)
                                    , this.DBAccess.CreateDbParameter("@in_cbilid", cbilid));
                            }
                            #endregion
                            info.Delete();
                        }
                    }
                    else
                    {
                        #region PDA删除收货数据时处理
                        if (!string.IsNullOrEmpty(info["ref_cbilid"].ToString()) && info["ref_cbilid"].ToString() != "-" && !string.IsNullOrEmpty(info["ref_ctabname"].ToString()) && info["ref_ctabname"].ToString() == "bl_cgdd_d1")
                        {
                            var sql = @"update  bl_cgdd_d1 set 
	                    fturnqty= ISNULL(CASE WHEN sh.fqty+sh.frecrejectqty>bl_cgdd_d1.fqty THEN bl_cgdd_d1.fqty ELSE sh.fqty+sh.frecrejectqty END,0)-CONVERT(VARCHAR(50),@in_fqty), 
	                    frecqty= ISNULL(CASE WHEN sh.fqty>bl_cgdd_d1.fqty THEN bl_cgdd_d1.fqty ELSE sh.fqty END,0)-CONVERT(VARCHAR(50),@in_fqty),
	                    frecvalue= ROUND((ISNULL(CASE WHEN sh.fqty>bl_cgdd_d1.fqty THEN bl_cgdd_d1.fqty ELSE sh.fqty END,0)- CONVERT(VARCHAR(50),@in_fqty))*fprice, convert(int,@valuedeclen)),
	                    frecrejectqty=ISNULL(sh.frecrejectqty,0),
	                    frecrejectvalue= ROUND(ISNULL(sh.frecrejectqty,0)*fprice,convert(int,@valuedeclen))
                    FROM bl_cgdd_d1
                    LEFT JOIN 
                    (SELECT bl_shd_d1.ref_cbilid,bl_shd_d1.ref_iid,
	                    SUM(fqty) AS fqty,SUM(case when bl_shd.iflag<100 then frecrejectqty else 0 end)+ISNULL(MAX(js.fcancelqty),0) AS frecrejectqty
	                    FROM bl_shd(nolock) 
	                    INNER JOIN bl_shd_d1(nolock) ON bl_shd_d1.cbilid = bl_shd.cbilid
	                    LEFT JOIN (
	                        SELECT orig_cbilid,orig_iid,SUM(fcancelqty) AS fcancelqty
	                        FROM 
	                        (
		                        SELECT d1.orig_cbilid,d1.orig_iid,SUM(case when bt.iflag>=100 THEN 0 ELSE d1.fqty end) AS fcancelqty 
		                        FROM dbo.bl_jsd(nolock) bt 
		                        INNER JOIN dbo.bl_jsd_d1(nolock) d1 ON d1.cbilid = bt.cbilid and d1.ref_cbiltype='SH'
		                        WHERE EXISTS(SELECT 1 FROM bl_shd_d1(nolock) p1 INNER JOIN bl_shd p ON p.cbilid = p1.cbilid 
						                    WHERE p.iflag = 100 AND p1.orig_cbilid = d1.orig_cbilid AND p1.orig_iid = d1.orig_iid
						                    and p1.orig_cbilid =@in_refcbilid and p1.orig_iid=@in_refiid
						                    )
		                        GROUP BY d1.orig_cbilid,d1.orig_iid
		                        UNION ALL
		                        SELECT d1.orig_cbilid,d1.orig_iid,SUM(d1.fqty) AS fcancelqty 
		                        FROM dbo.bl_jsd(nolock) bt 
		                        INNER JOIN dbo.bl_jsd_d1(nolock) d1 ON d1.cbilid = bt.cbilid and d1.ref_cbiltype='SH'
		                        WHERE bt.iflag = 100 
		                        AND EXISTS(SELECT 1 FROM bl_shd_d1(nolock) p1 WHERE p1.orig_cbilid = d1.orig_cbilid AND p1.orig_iid = d1.orig_iid
						                    and p1.orig_cbilid =@in_refcbilid and p1.orig_iid=@in_refiid
		                        )
		                        GROUP BY d1.orig_cbilid,d1.orig_iid
	                        )js1 GROUP BY orig_cbilid,orig_iid
	                    ) js ON js.orig_cbilid = bl_shd_d1.ref_cbilid AND js.orig_iid = bl_shd_d1.ref_iid
	                    WHERE bl_shd.ishtype = 1 AND bl_shd.iflag <> 999
	                        AND bl_shd_d1.cbilid<>@in_cbilid 
	                        AND bl_shd_d1.ref_cbilid =@in_refcbilid and bl_shd_d1.orig_iid=@in_refiid
	                    GROUP BY bl_shd_d1.ref_cbilid,bl_shd_d1.ref_iid
                    ) sh 
                    on bl_cgdd_d1.cbilid = sh.ref_cbilid AND bl_cgdd_d1.id1 = sh.ref_iid
                    WHERE cbilid=@in_refcbilid and id1=@in_refiid";
                            this.DBAccess.ExecuteNonQuery(sql, tran
                                , this.DBAccess.CreateDbParameter("@in_reftabname", info["ref_ctabname"].ToString())
                                , this.DBAccess.CreateDbParameter("@in_fqty", ConvertHelper.ToDecimal(info["fqty"]))
                                , this.DBAccess.CreateDbParameter("@valuedeclen", valueFormatlen)
                                , this.DBAccess.CreateDbParameter("@in_refcbilid", info["ref_cbilid"].ToString())
                                , this.DBAccess.CreateDbParameter("@in_refiid", info["ref_iid"].ToString())
                                , this.DBAccess.CreateDbParameter("@in_cbilid", cbilid));
                        }
                        #endregion
                        info.Delete();
                    }
                }
                this.DBAccess.Save(dt, tran);
                var toopBarProcResponse = this.ExecuteOther(
                       new BillToolBarProcActionRequest()
                       {
                           ModuleID = "DMSM01001210",
                           BillNO = cbilid,
                           BillType = "SH",
                           UserID = request.EmpCode,
                           ActionType = "save", //function.ActionType,
                           Action = "save-ok", // overwriteAction != null ? overwriteAction : function.Action,
                           Note = "",
                           OrgID = corgid.ToString(),
                           Cirter = request.EmpCode,
                           BillTypeName = "", // xop.WindowsTitle,
                           transaction = tran,
                           async = false,
                           MenuCode = "",//2018-7-30 ZGH
                           MenuName = "",
                           InvokeType = 2
                       }
                       );
                if (toopBarProcResponse.IsError || !toopBarProcResponse.Result.Success)
                {
                    tran.RollBack();
                }
                tran.Commit();
                #endregion
            }
            else if (request.OPType == 12)
            {
                #region 更新来货单号
                if (request.cbilid == null || request.cbilid == "" || request.QueryText == null || request.QueryText == "")
                {
                    response.IsError = true;
                    response.ErrorMessage = "参数不完整！";
                    return response;
                }
                var count = this.DBAccess.ExecuteNonQuery("update bl_shd set crecbilid=@crecbilid where cbilid=@cbilid "
                    , this.DBAccess.CreateDbParameter("@cbilid", request.cbilid), this.DBAccess.CreateDbParameter("@crecbilid", request.QueryText));
                if (count == 0)
                {
                    response.IsError = true;
                    response.ErrorMessage = "来货单号更新失败，受影响行数为0，请刷新数据！";
                    return response;
                }
                #endregion
            }

        retrueResponse:
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
                    SysLog.WriteLocalLog("第零行在这里", "PDA改造日志！！！");
                    head.Rows[0][sumColumn] = value;
                }
            }
        }
    }
}

