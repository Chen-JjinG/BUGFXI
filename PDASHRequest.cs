
/*********************************************************************************************************************/
//代码编写规则：
//1、定义引用程序集，格式必须为//reference:引用程序集文件名称(若需要引用外部程序集，请把外部程序集添加到程序运行目录下并且用{0}代替路径)
//2、名称必须唯一
//3、必须继承AuthorizeRequest<TResponse>
//4、类名必须加上特性[System.Runtime.Serialization.DataContract]
//5、属性必须加上特性[System.Runtime.Serialization.DataMember]
/*********************************************************************************************************************/

//reference:System.dll
//reference:System.Core.dll
//reference:System.Data.dll
//reference:System.Xml.dll
//reference:System.Xml.Linq.dll
//reference:System.Runtime.Serialization.dll
//reference:{0}KingBos.Infrastructure.dll


#region PDASHOPRequest

/* ------------------------------------------------
版本记录      版本日期      编辑人      编辑内容
V1.2        2025-08-05      CJJ       BUG#59662增加显示生产许可证
--------------------------------------------------- */

namespace KingBos.Infrastructure.Request
{

    #region 引用
    using KingBos.Infrastructure.Response;
    using KingBos.Infrastructure.Model;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    #endregion

    public class PDASHOPRequest : PDABaseTraceRequest<PDAWMSSHOPResponse>, IWebApiRequest
    {
        /// <summary>
        /// 操作类型 0查询列表 
        /// </summary>
        [System.Runtime.Serialization.DataMember]
        public int OPType { get; set; }
        /// <summary>
        /// 员工编码
        /// </summary>
        [System.Runtime.Serialization.DataMember]
        public string EmpCode { get; set; }

        /// <summary>
        /// 机构编码
        /// </summary>
        [System.Runtime.Serialization.DataMember]
        public string OrgID { get; set; }
        /// <summary>
        /// 仓库编码
        /// </summary>
        [System.Runtime.Serialization.DataMember]
        public string cckid { get; set; }
        /// <summary>
        /// 数据来源
        /// </summary>
        [System.Runtime.Serialization.DataMember]
        public int ishtype { get; set; }
        /// <summary>
        /// 往来单位
        /// </summary>
        [System.Runtime.Serialization.DataMember]
        public string ccorpid { get; set; }
        /// <summary>
        /// 单据编号
        /// </summary>
        [System.Runtime.Serialization.DataMember]
        public string cbilid { get; set; }
        /// <summary>
        /// 查询文本
        /// </summary>
        [System.Runtime.Serialization.DataMember]
        public string QueryText { get; set; }
        /// <summary>
        /// 收货录入信息
        /// </summary>
        [System.Runtime.Serialization.DataMember]
        public List<SHGoods> LRInfoList { get; set; }
        /// <summary>
        /// 冷藏信息
        /// </summary>
        [System.Runtime.Serialization.DataMember]
        public ColdStoreInfo ColdStore { get; set; }
        /// <summary>
        /// 单据信息
        /// </summary>

        [System.Runtime.Serialization.DataMember]
        public SHMainInfo SHInfo { get; set; }


    }
}

namespace KingBos.Infrastructure.Response
{
    #region 引用
    using KingBos.Infrastructure.Response;
    using KingBos.Infrastructure.Model;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    #endregion
    [System.Runtime.Serialization.DataContract]
    public class PDAWMSSHOPResponse : PDABaseTraceResponse<List<SHMainInfo>>
    {
        [System.Runtime.Serialization.DataMember]
        public List<StoreInfo> StoreInfoList { get; set; }

        [System.Runtime.Serialization.DataMember]
        public List<CorpInfo> CorpInfoList { get; set; }

        [System.Runtime.Serialization.DataMember]
        public List<SHGoods> SHGoodsList { get; set; }

        [System.Runtime.Serialization.DataMember]
        public List<RejectionReason> RejectionReasonsList { get; set; }

        

    }
}

namespace KingBos.Infrastructure.Model
{
    #region 引用
    using KingBos.Infrastructure.Response;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    #endregion
    public class SHMainInfo
    {
        /// <summary>
        /// 单据编号
        /// </summary>
        public string cbilid { get; set; }
        /// <summary>
        /// 单据日期
        /// </summary>
        public string dbildate { get; set; }
        /// <summary>
        /// 单据类型
        /// </summary>
        public string cbiltype { get; set; }
        /// <summary>
        /// 归属公司
        /// </summary>
        public string corgid { get; set; }
        /// <summary>
        /// 归属部门
        /// </summary>
        public string cdeptid { get; set; }
        /// <summary>
        /// 仓库编码
        /// </summary>
        public string cckid { get; set; }
        /// <summary>
        /// 数据来源
        /// </summary>
        public int ishtype { get; set; }
        /// <summary>
        /// 经手人
        /// </summary>
        public string chandler { get; set; }
        /// <summary>
        /// 业务组
        /// </summary>
        public string csalerid { get; set; }
        /// <summary>
        /// 往来单位
        /// </summary>
        public string ccorpid { get; set; }
        /// <summary>
        /// 目标机构
        /// </summary>
        public string ctagorgid { get; set; }
        /// <summary>
        /// 来货单号
        /// </summary>
        public string crecbilid { get; set; }
        /// <summary>
        /// 收货员
        /// </summary>
        public string crecer { get; set; }
        /// <summary>
        /// 收货开始作业时间
        /// </summary>
        public string dstartdate { get; set; }
        /// <summary>
        /// 是否委托单据
        /// </summary>
        public int ientrustflag { get; set; }
        /// <summary>
        /// 源单号
        /// </summary>
        public string ref_cbilid { get; set; }
        /// <summary>
        /// 源单据类型
        /// </summary>
        public string ref_cbiltype { get; set; }
        /// <summary>
        /// 源表名
        /// </summary>
        public string ref_ctabname { get; set; }
        /// <summary>
        /// 冷藏状态
        /// </summary>
        public int irefrigerateflag { get; set; }
        /// <summary>
        /// 合计数量
        /// </summary>
        public decimal fhead_qty { get; set; }
        /// <summary>
        /// 合计金额
        /// </summary>
        public decimal fhead_value { get; set; }
        /// <summary>
        /// 状态  
        /// </summary>
        public int iflag { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string cnote { get; set; }
        /// <summary>
        /// 经手人名称
        /// </summary>
        public string cempname { get; set; }
        /// <summary>
        /// 仓库名称
        /// </summary>
        public string cckname { get; set; }
        /// <summary>
        /// 机构名称
        /// </summary>
        public string corgname { get; set; }
        /// <summary>
        /// 往来单位名称
        /// </summary>
        public string ccorpname { get; set; }
        /// <summary>
        /// 往来单位地址
        /// </summary>
        public string caddress { get; set; }
    }

    public class SHGoods
    {
    	/// <summary>
        /// 生产许可证 V1.2  
        /// </summary>
        [System.Runtime.Serialization.DataMember]
        public string ccertificateno { get; set; }
        /// <summary>
        /// 商品编码
        /// </summary>
        public string cgoodsid { get; set; }
        /// <summary>
        /// 商品名称
        /// </summary>
        public string cgoodsname { get; set; }
        /// <summary>
        /// 条码
        /// </summary>
        public string cbarcode { get; set; }
        /// <summary>
        /// 产地
        /// </summary>
        public string cprodaddress { get; set; }
        /// <summary>
        /// 批准文号
        /// </summary>
        public string cfileno { get; set; }
        /// <summary>
        /// 规格
        /// </summary>
        public string cpkname { get; set; }
        /// <summary>
        /// 生产厂家
        /// </summary>
        public string cfactoryname { get; set; }
        /// <summary>
        /// 助记码
        /// </summary>
        public string czjmcode { get; set; }
        /// <summary>
        /// 单位
        /// </summary>
        public string cunit { get; set; }
        /// <summary>
        /// 保质期
        /// </summary>
        public int iterm { get; set; }
        /// <summary>
        /// 长
        /// </summary>
        public decimal fpklong { get; set; }
        /// <summary>
        /// 宽
        /// </summary>
        public decimal fpkwidth { get; set; }
        /// <summary>
        /// 高
        /// </summary>
        public decimal fpkheight { get; set; }
        /// <summary>
        /// 体积
        /// </summary>
        public decimal fpkvolume { get; set; }
        /// <summary>
        /// 重量
        /// </summary>
        public decimal fpkweight { get; set; }
        /// <summary>
        /// 是否管理批号
        /// </summary>
        public int iphflag { get; set; }
        /// <summary>
        /// 批号
        /// </summary>
        public string cph { get; set; }
        /// <summary>
        /// 数据来源
        /// </summary>
        public int ishtype { get; set; }
        private string _dmadedate;
        private string _dexpdate;
        /// <summary>
        /// 生产日期
        /// </summary>
        public string dmadedate { get { return _dmadedate; } set { this._dmadedate = !string.IsNullOrWhiteSpace(value) ? Convert.ToDateTime(value).ToString("yyyy-MM-dd") : value; } }
        /// <summary>
        /// 有效期
        /// </summary>
        public string dexpdate { get { return _dexpdate; } set { this._dexpdate = !string.IsNullOrWhiteSpace(value) ? Convert.ToDateTime(value).ToString("yyyy-MM-dd") : value; ; } }
        
        /// <summary>
        /// 批号备注
        /// </summary>
        [System.Runtime.Serialization.DataMember]
        public string cphnote { get; set; }

        /// <summary>
        /// 所属注册证号
        /// </summary>
        [System.Runtime.Serialization.DataMember]
        public string cphnote2 { get; set; }

        /// <summary>
        /// 中药辅助编码
        /// </summary>
        [System.Runtime.Serialization.DataMember]
        public string cphcmaidcode { get; set; }
        /// <summary>
        /// 往来单位
        /// </summary>
        public string ccorpid { get; set; }
        /// <summary>
        /// 往来单位名称
        /// </summary>
        public string ccorpname { get; set; }
        /// <summary>
        /// 单据编号
        /// </summary>
        public string cbilid { get; set; }
        private string _dbildate;
        /// <summary>
        /// 单据日期
        /// </summary>
        public string dbildate { get { return _dbildate; } set { this._dbildate = !string.IsNullOrWhiteSpace(value) ? Convert.ToDateTime(value).ToString("yyyy-MM-dd") : value; ; } }
        /// <summary>
        /// 序号
        /// </summary>
        public int id1 { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        public decimal fqty { get; set; }
        /// <summary>
        /// 单价
        /// </summary>
        public decimal fnormprice { get; set; }
        /// <summary>
        /// 来源单价
        /// </summary>
        public decimal ref_fprice { get; set; }
        /// <summary>
        /// 金额
        /// </summary>
        public decimal fnormvalue { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string cnote { get; set; }
        /// <summary>
        /// 收货拒收数量
        /// </summary>
        public decimal frecrejectqty { get; set; }
        /// <summary>
        /// 收货拒收金额
        /// </summary>
        public decimal frecrejectvalue { get; set; }
        /// <summary>
        /// 通知数量
        /// </summary>
        public decimal ref_fqty { get; set; }
        /// <summary>
        /// 拒收原因
        /// </summary>
        public string crejectseason { get; set; }
        /// <summary>
        /// 收货人
        /// </summary>
        public string crecer { get; set; }
        /// <summary>
        /// 数据来源类型
        /// </summary>
        public string cbiltype { get; set; }
        /// <summary>
        /// 发运地址
        /// </summary>
        public string csendadd { get; set; }
        private string _dsendstart;
        /// <summary>
        /// 启运时间
        /// </summary>
        public string dsendstart { get { return _dsendstart; } set { this._dsendstart = !string.IsNullOrWhiteSpace(value) ? Convert.ToDateTime(value).ToString("yyyy-MM-dd HH:mm:ss") : value; ; } }

        public string _drectime;
        /// <summary>
        /// 到货时间
        /// </summary>
        public string drectime { get { return _drectime; } set { this._drectime = !string.IsNullOrWhiteSpace(value) ? Convert.ToDateTime(value).ToString("yyyy-MM-dd HH:mm:ss") : value; ; } }
        /// <summary>
        /// 运输时长(小时)
        /// </summary>
        public decimal fhours { get; set; }
        /// <summary>
        /// 到货温度
        /// </summary>
        public string ctep { get; set; }
        /// <summary>
        /// 运输方式
        /// </summary>
        public string csendtype { get; set; }
        /// <summary>
        /// 温控方式
        /// </summary>
        public string ctepctl { get; set; }
        /// <summary>
        /// 温控状况
        /// </summary>
        public string ctepnote { get; set; }
        /// <summary>
        /// 运输过程温度是否符合
        /// </summary>
        public string ctepsta { get; set; }
        /// <summary>
        /// 运输车辆
        /// </summary>
        public string ccar { get; set; }
        /// <summary>
        /// 车牌号码
        /// </summary>
        public string ccarno { get; set; }
        /// <summary>
        /// 送货人
        /// </summary>
        public string csender { get; set; }
        /// <summary>
        /// 运输人
        /// </summary>
        public string ctranser { get; set; }
        /// <summary>
        /// 托运单位
        /// </summary>
        public string ctransport { get; set; }
        /// <summary>
        /// 到货湿度
        /// </summary>
        public string cdap { get; set; }
        /// <summary>
        /// 启运温度
        /// </summary>
        public string cstep { get; set; }
        /// <summary>
        /// 启运湿度
        /// </summary>
        public string csdap { get; set; }
        /// <summary>
        /// 是否冷藏商品
        /// </summary>
        public int isgspcold { get; set; }
        /// <summary>
        /// 折扣率
        /// </summary>
        public decimal fdiscrate { get; set; }
        /// <summary>
        /// 折扣金额
        /// </summary>
        public decimal fdiscvalue { get; set; }
        /// <summary>
        /// 折后单价
        /// </summary>
        public decimal fprice { get; set; }
        /// <summary>
        /// 折后金额
        /// </summary>
        public decimal fvalue { get; set; }
        /// <summary>
        /// 税率
        /// </summary>
        public decimal ftaxrate { get; set; }
        /// <summary>
        /// 税额
        /// </summary>
        public decimal ftaxvalue { get; set; }
        /// <summary>
        /// 不含税单价
        /// </summary>
        public decimal fnotaxprice { get; set; }
        /// <summary>
        /// 不含税金额
        /// </summary>
        public decimal fnotaxvalue { get; set; }
        /// <summary>
        /// 开票单价
        /// </summary>
        public decimal finvprice { get; set; }
        /// <summary>
        /// 开票金额
        /// </summary>
        public decimal finvvalue { get; set; }
        /// 币种
        /// </summary>
        public string cbz { get; set; }
        /// <summary>
        /// 原始表名
        /// </summary>
        public string orig_ctabname { get; set; }
        /// <summary>
        /// 原始单据类型
        /// </summary>
        public string orig_cbiltype { get; set; }
        /// <summary>
        /// 原始单号
        /// </summary>
        public string orig_cbilid { get; set; }
        /// <summary>
        /// 原序号
        /// </summary>
        public string orig_iid { get; set; }
        /// <summary>
        /// 源单号
        /// </summary>
        public string ref_cbilid { get; set; }
        /// <summary>
        /// 源单据类型
        /// </summary>
        public string ref_cbiltype { get; set; }
        /// <summary>
        /// 源表名
        /// </summary>
        public string ref_ctabname { get; set; }
        /// <summary>
        /// 源序号
        /// </summary>
        public string ref_iid { get; set; }
        /// <summary>
        /// 整件+散件收货
        /// </summary>
        public string chwcode { get; set; }
        /// <summary>
        /// 收货数量
        /// </summary>
        [System.Runtime.Serialization.DataMember]
        public decimal frecqty { get; set; }
       
        /// <summary>
        /// 批号信息
        /// </summary>
        [System.Runtime.Serialization.DataMember]
        public List<SHPHInfo> PHList;
        /// <summary>
        /// 所属商品注册证
        /// </summary>
        [System.Runtime.Serialization.DataMember]
        public List<KingBos.Infrastructure.Request.PDAGoodscfilenofilelistRequest> cfilenofilelist { get; set; }
    }
    public class SHPHInfo
    {
        /// <summary>
        /// 商品编码
        /// </summary>
        public string cgoodsid { get; set; }
        /// <summary>
        /// 商品名称
        /// </summary>
        public string cgoodsname { get; set; }
        /// <summary>
        /// 批号
        /// </summary>
        public string cph { get; set; }
        /// <summary>
        /// 通知数量
        /// </summary>
        public decimal fqty { get; set; }
        /// <summary>
        /// 收货数量
        /// </summary>
        public decimal fshqty { get; set; }
        /// <summary>
        /// 拒收数量
        /// </summary>
        public decimal fqsqty { get; set; }
        /// <summary>
        /// 拒收原因
        /// </summary>
        public string crejectseason { get; set; }

        private string _dmadedate;
        private string _dexpdate;
        /// <summary>
        /// 生产日期
        /// </summary>
        public string dmadedate { get { return _dmadedate; } set { this._dmadedate = !string.IsNullOrWhiteSpace(value) ? Convert.ToDateTime(value).ToString("yyyy-MM-dd") : value; } }
        /// <summary>
        /// 有效期
        /// </summary>
        public string dexpdate { get { return _dexpdate; } set { this._dexpdate = !string.IsNullOrWhiteSpace(value) ? Convert.ToDateTime(value).ToString("yyyy-MM-dd") : value; ; } }
        
        /// <summary>
        /// 批号备注
        /// </summary>
        [System.Runtime.Serialization.DataMember]
        public string cphnote { get; set; }

        /// <summary>
        /// 所属注册证号
        /// </summary>
        [System.Runtime.Serialization.DataMember]
        public string cphnote2 { get; set; }

        /// <summary>
        /// 中药辅助编码
        /// </summary>
        [System.Runtime.Serialization.DataMember]
        public string cphcmaidcode { get; set; }
        /// <summary>
        /// 灭菌批号
        /// </summary>
        public string cmjph { get; set; }
        /// <summary>
        /// 保质期
        /// </summary>
        public int iterm { get; set; }
        /// <summary>
        /// 是否管理批号
        /// </summary>
        public int iphflag { get; set; }
        /// <summary>
        /// 记账预留字段1
        /// </summary>
        public string cjzyl1 { get; set; }
        /// <summary>
        /// 记账预留字段2
        /// </summary>
        public string cjzyl2 { get; set; }

    }
    public class RejectionReason
    {
        /// <summary>
        /// 拒绝原因编码
        /// </summary>
        public string ccodevalue { get; set; }
        /// <summary>
        /// 拒绝原因
        /// </summary>
        public string ccodetext { get; set; }
        /// <summary>
        ///备注
        /// </summary>
        public string cnote { get; set; }
    }
    public class CorpInfo
    {
        /// <summary>
        /// 类型
        /// </summary>
        public string ctype { get; set; }
        /// <summary>
        /// 往来单位编码
        /// </summary>
        public string ccorpid { get; set; }
        /// <summary>
        /// 往来单位名称
        /// </summary>
        public string ccorpname { get; set; }
        /// <summary>
        /// 地址
        /// </summary>
        public string caddress { get; set; }
        /// <summary>
        /// 助记码
        /// </summary>
        public string czjmcode { get; set; }
    }

    public class ColdStoreInfo
    {
        public string cbiltype { get; set; }
        /// <summary>
        /// 发运地址
        /// </summary>
        public string csendadd { get; set; }
        private string _dsendstart;
        /// <summary>
        /// 启运时间
        /// </summary>
        public string dsendstart { get { return _dsendstart; } set { this._dsendstart = !string.IsNullOrWhiteSpace(value) ? Convert.ToDateTime(value).ToString("yyyy-MM-dd HH:mm:ss") : value; ; } }

        public string _drectime;
        /// <summary>
        /// 到货时间
        /// </summary>
        public string drectime { get { return _drectime; } set { this._drectime = !string.IsNullOrWhiteSpace(value) ? Convert.ToDateTime(value).ToString("yyyy-MM-dd HH:mm:ss") : value; ; } }
        /// <summary>
        /// 运输时长(小时)
        /// </summary>
        public decimal fhours { get; set; }
        /// <summary>
        /// 到货温度
        /// </summary>
        public string ctep { get; set; }
        /// <summary>
        /// 运输方式
        /// </summary>
        public string csendtype { get; set; }
        /// <summary>
        /// 温控方式
        /// </summary>
        public string ctepctl { get; set; }
        /// <summary>
        /// 温控状况
        /// </summary>
        public string ctepnote { get; set; }
        /// <summary>
        /// 运输过程温度是否符合
        /// </summary>
        public string ctepsta { get; set; }
        /// <summary>
        /// 运输车辆
        /// </summary>
        public string ccar { get; set; }
        /// <summary>
        /// 车牌号码
        /// </summary>
        public string ccarno { get; set; }
        /// <summary>
        /// 送货人
        /// </summary>
        public string csender { get; set; }
        /// <summary>
        /// 运输人
        /// </summary>
        public string ctranser { get; set; }
        /// <summary>
        /// 托运单位
        /// </summary>
        public string ctransport { get; set; }
        /// <summary>
        /// 到货湿度
        /// </summary>
        public string cdap { get; set; }
        /// <summary>
        /// 启运温度
        /// </summary>
        public string cstep { get; set; }
        /// <summary>
        /// 启运湿度
        /// </summary>
        public string csdap { get; set; }
    }
}
#endregion



