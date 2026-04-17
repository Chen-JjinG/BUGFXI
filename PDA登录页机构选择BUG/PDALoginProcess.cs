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
 
 
#region PDALoginProcess

/* ------------------------------------------------
版本记录      版本日期      编辑人      编辑内容
V1.1          2026-01-20    CJJ       增加批量采码管控值
V1.2          2026-03-16    CJJ         增加追溯码匹配管控值 bug#75647
V1.3          2026-04-07    ZZH         增加动盘
--------------------------------------------------- */

namespace KingBos.WMSBusiness 
{ 

    #region 引用
    using KingBos.Business.Core; 
    using KingBos.Infrastructure.Request; 
    using KingBos.Infrastructure.Response; 
    using System; 
    using System.Collections.Generic; 
    using System.Linq; 
    using System.Text; 
    using KingBos.Infrastructure.Model; 
    using KingBos.Infrastructure.Utility; 
    using System.Data; 
    using KingBos.Business.Context; 
    #endregion 
 
    /// <summary> 
    /// WMS_PDA用户登录 2017-10-28 ZGH 
    /// </summary> 
    public class PDALoginProcess : BaseRequestProcesser<PDALoginRequest> 
    { 
        protected override BaseResponse Execute(PDALoginRequest request) 
        { 
            var response = this.CreateResponse(request); 
            var findAccountRow = this.SettingDBCache.Get("AccountInfo").Select("Name='" + request.AccountName + "'").FirstOrDefault(); 
            if (findAccountRow == null) 
            { 
                response.IsError = true; 
                response.ErrorMessage = "找不到此账套。"; 
            } 
            else 
            { 
                SessionContext.Current.SetDBAccess(request.AccountName);
                switch (request.OperationType) 
                { 
                    case 0://用户登录 
                        response.Result = Login(request.AccountName, request.Name, request.Pwd, request.MachineCode,request.OrgID, request.DeptID); 
                        //TODO 验证是否允许同账号多次登陆 
 
                        break; 
                    default: 
                        break; 
                } 
                return response; 
            } 
            return response; 
        } 
 
        #region 账户登录 
 
        private PDALoginState Login(string accountName, string name, string pwd, string machinecode,string orgid, string deptid) 
        { 
            var loginState = new PDALoginState() { State = new Result() }; 
            //if (this.SettingDBCache.Get("AccountInfo").Select("Name='" + accountName + "'").Length == 0) 
            //{ 
            //    loginState.State.Message = "找不到此账套。"; 
            //    return loginState; 
            //} 
            var TokenRows = this.DBCache.Get("sy_app_auth").Select("ccode ='android'OR cname='android'"); 
            if(TokenRows==null)
            {
            loginState.State.Message = "登录失败!访问令牌不能为空"; 
            return loginState; 
            }

            //SessionContext.Current.SetDBAccess(accountName); 
            //支持账号或名称登录 
            var findRows = this.DBCache.Get("sy_user").Select(string.Format("(cempid='{0}' or cempname='{0}') and cpwd='{1}' ", name, DESEncrypt.Encrypt(pwd))); 
            //if (findRows.Length > 0)
            //{ 
            //    findRows = this.DBCache.Get("sy_user").Select(string.Format("cempid='{0}' and cpwd='{1}'", findRows[0]["cempid"], DESEncrypt.Encrypt(pwd))); 
            //} 
            loginState.Info = new UserInfo(); 
            if (findRows.Length > 0) 
            {
                var finduserRow = findRows.Where(r => Convert.ToInt32(r["iflag"]) == 10).FirstOrDefault();
                //校验员工信息状态是否正常
                if (findRows.Length > 1)//如果存在多条记录，则判断是否停用
                {
                    foreach (var item in findRows)
                    {
                        if (ConvertHelper.ToInt( item["iflag"]) == 10)
                        {
                          var findowemploee=   this.DBCache.Get("bf_employee").Select("cempid='" + item["cempid"].ToString() + "' and iflag=100").FirstOrDefault();
                          if (findowemploee != null)
                          {
                              finduserRow = findRows.Where(r => r["cempid"].ToString() == item["cempid"].ToString()).FirstOrDefault();
                              break;
                          }
                        }
                    }
                }
                //var finduserRow = findRows.Where(r => Convert.ToInt32(r["iflag"]) == 10).FirstOrDefault();
                if (finduserRow == null)
                {
                    finduserRow = findRows[0];
                }
                
                var userAccount = Sy_UserAccount.Create(finduserRow); 
                if (userAccount.iflag != 10) 
                { 
                    loginState.State.Message = "登录失败!该账户状态为【" + userAccount.FlagText + "】，登录失败。"; 
                } 
              
                else 
                { 
                    //var findUserRows = this.DBCache.Get("sy_roleuserdef").Select(string.Format("iroleuserid='{0}'", userAccount.iuserid)); 
                    //if (findUserRows.Length == 0) 
                    //{ 
                    //    loginState.State.Message = "登录失败! 找不到此账户对应的用户信息，登录失败。"; 
                    //} 
                    //else 
                    //{ 
                    //    var user = Sy_RoleUserDef.Create(findUserRows[0]); 
                    //    if (user.iflag != 10) 
                    //    { 
                    //        loginState.State.Message = "登录失败! 该用户状态为【" + user.FlagText + "】，登录失败。"; 
                    //    } 
                    //    else 
                    //    { 
                            var findEmloyeeRow = this.DBCache.Get("bf_employee").Select("cempid='" + userAccount.cygno + "' and iflag=100").FirstOrDefault();
                            if (findEmloyeeRow == null) findEmloyeeRow = this.DBCache.Get("bf_employee").Select("cempid='" + userAccount.cygno + "'").FirstOrDefault(); 
                            if (findEmloyeeRow == null) 
                            { 
                                loginState.State.Message = "登录失败! 此账户未关联员工信息，登录失败。"; 
                            } 
                            else 
                            { 
                                if (findEmloyeeRow["iflag"].ToString() != "100" && userAccount.cuseracctno != "admin") 
                                { 
                                    loginState.State.Message = "登录失败! 此账户员工状态不正常，登录失败。"; 
                                    return loginState; 
                                }
                                if (findEmloyeeRow["iflag"].ToString() != "100" && userAccount.cuseracctno != "admin") 
                                { 
                                    loginState.State.Message = "登录失败! 此账户员工状态不正常，登录失败。"; 
                                    return loginState; 
                                }
                                var userRow = findRows.FirstOrDefault(r => Convert.ToInt32(r["iflag"]) == 10);
                                if (userRow == null)
                                {
                                    userRow = findRows[0];
                                }
                                if (!string.IsNullOrWhiteSpace(orgid) && Convert.ToInt32(userRow["iuserid"]) != 1)
                                {
                                    var orgID = findEmloyeeRow["corgid"].ToString();
                                    if (orgid != orgID)
                                    {
                                        if (this.DBCache.Get("sy_userorg").Select("iuserid=" + userRow["iuserid"] + " and corgid='" + orgid + "'").Length == 0)
                                        {
                                            loginState.State.Message = "登录失败! 该用户不允许登录此机构。";
                                            return loginState;
                                        }
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrWhiteSpace(deptid))
                                        {
                                            if (deptid != findEmloyeeRow["cdeptid"].ToString())
                                            {
                                                loginState.State.Message = "登录失败! 该用户不允许登录此部门。";
                                                return loginState;
                                            }
                                        }

                                    }
                                }
                                var serverDate = this.DBAccess.GetDataTable("select getdate()"); 
                                var orgRow = this.DBCache.Get("bf_org").Select("corgid='" + orgid + "'").FirstOrDefault(); 
                                var depRow = this.DBCache.Get("bf_org").Select("corgid='" + deptid + "'").FirstOrDefault(); 
 
                                #region 设置用户登录信息 
 
                                loginState.Info = new UserInfo() 
                                { 
                                    Token = null, 
                                    AccountCode = accountName, 
                                    AccountName = accountName, 
                                    ID = userAccount.iuserid, 
                                    //Name = user.cname, 
                                   // UserAccountName = userAccount.cusername, 
                                   // UserAccountNO = userAccount.cuseracctno, 
                                    OrgID = findEmloyeeRow["corgid"].ToString(), 
                                    OrgType = orgRow == null ? -1 : Convert.ToInt32(orgRow["itype"]), 
                                    OrgName = orgRow == null ? "" : orgRow["corgname"].ToString(), 
                                    OrgFullName = orgRow == null ? "" : orgRow["ccompanyname"].ToString(), 
                                    OrgTel = orgRow == null ? "" : orgRow["cphone"].ToString(), 
                                    OrgAddress = orgRow == null ? "" : orgRow["caddress"].ToString(),
                                    DeptID = deptid,
                                    //DeptName = depRow == null ? "" : depRow["corgname"].ToString(),
                                    Now = Convert.ToDateTime(serverDate.Rows[0][0]), 
                                    EmployeeCode = userAccount.cygno, 
                                    EmployeeName = findEmloyeeRow["cempname"].ToString(), 
                                    //MenuGroupIDs = user.MenuGroupIDs, 
                                    //HasCostPermission = PermissionHelper.HasCostPermission(user.iroleuserid),

                                    IsWMrechk = findEmloyeeRow.Table.Columns.Contains("iwmrechk") && Convert.ToInt32(findEmloyeeRow["iwmrechk"]) == 1,
                                    IsCMrechk = findEmloyeeRow.Table.Columns.Contains("icmrechk") && Convert.ToInt32(findEmloyeeRow["icmrechk"]) == 1,
                                    IsWMmaintain = findEmloyeeRow.Table.Columns.Contains("iwmmaintain") && Convert.ToInt32(findEmloyeeRow["iwmmaintain"]) == 1,
                                    IsCMmmaintain = findEmloyeeRow.Table.Columns.Contains("icmmaintain") && Convert.ToInt32(findEmloyeeRow["icmmaintain"]) == 1,

                                    IsChk = findEmloyeeRow.Table.Columns.Contains("ichk") && (findEmloyeeRow["ichk"] == DBNull.Value ? 0 : Convert.ToInt32(findEmloyeeRow["ichk"])) == 1,
                                    IsSpegoodspur = findEmloyeeRow.Table.Columns.Contains("ispegoodspur") && Convert.ToInt32(findEmloyeeRow["ispegoodspur"]) == 1,
                                    IsSpegoodschk = findEmloyeeRow.Table.Columns.Contains("ispegoodschk") && Convert.ToInt32(findEmloyeeRow["ispegoodschk"]) == 1,
                                    IsSpegoodsrechk = findEmloyeeRow.Table.Columns.Contains("ispegoodsrechk") && Convert.ToInt32(findEmloyeeRow["ispegoodsrechk"]) == 1,
                                    //ctw 2020-04-22 一类特药不需要专人复核权限
                                    //IsSpegoodschk = true,
                                    //IsSpegoodsrechk = true,
                                    IsDoctor = findEmloyeeRow.Table.Columns.Contains("idoctor") && Convert.ToInt32(findEmloyeeRow["idoctor"]) == 1,
                                    //EnablePerformanceLoger = orgRow == null || !orgRow.Table.Columns.Contains("benabletrace") || orgRow["benabletrace"] == DBNull.Value ? false : Convert.ToBoolean(orgRow["benabletrace"]),
 
                                }; 
                                loginState.State.Success = true;
                        		#endregion
                        								
                                //系统配置数据 
                                var configDt = this.DBCache.Get("sy_config").Copy(); 
                               
                                foreach (DataRow row in configDt.Rows)
                                {
                                    //2024-03-06 判断是否存在货位管理功能,其他参数暂不处理
                                    //2025-04-25 PDA采码迭代相关参数强制重读数据库
                                    if (row["cparmname"].ToString() == "MUILTHWMANAGE" || row["cparmname"].ToString() == "MULTIHWORG" 
                                        || row["cparmname"].ToString() == "UseZsmNewProcess" || row["cparmname"].ToString()== "UDICombinationBarcodeMainLength")
                                    {
                                        row["cparmvalue"] = this.DBAccess.ExecuteScalar("SELECT dbo.fun_getconfig ('" + row["cparmname"].ToString() + "','" + orgid + "')");
                                    }
                                }

                                List<ConfigInfo> configInfos = (from DataRow row in configDt.Rows 
                                                                select new ConfigInfo() 
                                                                { 
                                                                    cparmname = row["cparmname"].ToString(), 
                                                                    cparmvalue = row["cparmvalue"].ToString(), 
                                                                    iflag = Convert.ToInt32(row["iflag"]) 
                                                                }).ToList(); 
 
                                loginState.ConfigList = configInfos;
                                var CompanyID = orgRow == null || orgRow["cblorgid"] == DBNull.Value ? "" : orgRow["cblorgid"].ToString();

                                //PDA功能模块权限
                                var pdaMenu = new List<string>() {
                                "PDA-盘点",
                                "PDA-收货",
                                "PDA-验收",
                                "PDA-库存查询",
                                "PDA-拣货",
                    			"PDA-机构商品货位查询",
                    			"PDA-实时盘点",
                    			"PDA-门店收货",
                    			"PDA-药检报告",
                                "PDA-采购入库",
                                "PDA-其他出库",
                                "PDA-库存移库",
                                "PDA-移库销售订单",
                                "PDA-销售移库",
								"PDA-上架",
								"PDA-移位",
								"PDA-下架",
                                "PDA-复核",
                                "PDA-货位盘点",//V1.1
                    			"PDA-批量采码",//V1.1
                    			"PDA-匹配追溯标识",//V1.2
                                "PDA-动盘",//V1.3
                                };
                                int order = -12;
                                loginState.ModuleList = new List<ModuleLimit>();
                                for (int i = 0; i < pdaMenu.Count; i++)
                                {
                                    order--;
                                    loginState.ModuleList.Add(new ModuleLimit()
                                    {
                                        cmoduleid= "PDA_" + i.ToString(),
                                        cmenuno = "PDA_" + i.ToString(),
                                        cmenuname= pdaMenu[i],
                                        iorder = order,
                                        accesspermission=PermissionHelper.HasOtherPermission(userAccount.iuserid, order, CompanyID)
                                    });
                                }

                                //编码对照配置数据 
                                //var lookupDt = this.DBCache.Get("sy_lookup").Copy(); 
                                ////lookupDt.DefaultView.RowFilter = "ctype = 'CARTYPE'"; 
                                //List<LookupInfo> lookupInfos = (from DataRow row in lookupDt.Rows 
                                //                                select new LookupInfo() 
                                //                                { 
                                //                                    ccodevalue = row["ccodevalue"].ToString(), 
                                //                                    ccodetext = row["ccodetext"].ToString(), 
                                //                                    ctype = row["ctype"].ToString(), 
                                //                                    iflag = Convert.ToInt32(row["iflag"]) 
                                //                                }).ToList(); 

                                //loginState.LookupList = lookupInfos; 

                                //PDA员工特殊权限 
                                //var spDt = this.DBCache.Get("bf_special_plan").Copy();
                                //spDt.DefaultView.RowFilter = "iroleuserid = '" + userAccount.iuserid + "'"; 
                                //var spInfoList = new List<PDAUserSpecialPlanInfo>(); 
                                //foreach (DataRowView dataRowView in spDt.DefaultView) 
                                //{ 
                                //    //lookupDt.DefaultView.RowFilter = "ccodevalue = '" + dataRowView["cbusinesstype"] + "'"; 
                                //    spInfoList.AddRange(from DataRowView rowView in spDt.DefaultView 
                                //                        select new PDAUserSpecialPlanInfo() 
                                //                        { 
                                //                            ccode = userAccount.cygno, 
                                //                            cname = findEmloyeeRow["cname"].ToString(),
                                //                            ccodevalue = rowView["crightcode"].ToString(), 
                                //                            ccodetext = rowView["ccodetext"].ToString(), 
                                //                            ctype = rowView["ctype"].ToString() 
                                //                        }); 

                                //     var spDt = this.DBCache.Get("sy_roleuserright").Copy();
                                //spDt.DefaultView.RowFilter = "iroleuserid = '" + userAccount.iuserid + "'";
                                //var spInfoList = new List<PDAUserSpecialPlanInfo>();
                                ////foreach (DataRowView dataRowView in spDt.DefaultView)
                                ////{
                                //    //lookupDt.DefaultView.RowFilter = "ccodevalue = '" + dataRowView["cbusinesstype"] + "'";
                                //    spInfoList.AddRange(from DataRowView rowView in spDt.DefaultView
                                //                        select new PDAUserSpecialPlanInfo()
                                //                        {
                                //                            ccode = userAccount.cygno,
                                //                            cname = findEmloyeeRow["cname"].ToString(),
                                //                            ccodevalue = rowView["crightcode"].ToString(),
                                //                            ccodetext = "",
                                //                            ctype = rowView["iflag"].ToString()
                                //                        }); 
                                //foreach (DataRowView rowView in lookupDt.DefaultView) 
                                //{ 
                                //    var spInf = new PDAUserSpecialPlanInfo() 
                                //    { 
                                //        ccode = userAccount.cygno, 
                                //        cname = findEmloyeeRow["cname"].ToString(), 
                                //        ccodevalue = rowView["ccodevalue"].ToString(), 
                                //        ccodetext = rowView["ccodetext"].ToString(), 
                                //        ctype = rowView["ctype"].ToString() 
                                //    }; 
                                //    spInfoList.Add(spInf); 
                                //} 
                                //} 
                                //loginState.PDAUserSpecialPlanInfo = spInfoList; 

                                // } 
                                //} 
                    } 
                } 
            } 
            else 
            { 
                loginState.State.Message = "登录失败! 账户或密码有误，请重新输入!"; 
            } 
 
            //if (loginState.State.Success) 
                //if (this.DBCache.Get("sy_loginfilter").Select("cmachinecode='" + machinecode + "'").Length == 0) 
                //{ 
                //    this.DBAccess.ExecuteNonQuery(@"INSERT INTO sy_machinecodenotes (cmachinecode, dlastupdtime,accno,ccode )VALUES(@cmachinecode,GETDATE(),@accno,@ccode)", 
                //                                this.DBAccess.CreateDbParameter("@cmachinecode", machinecode), 
                //                                this.DBAccess.CreateDbParameter("@accno", loginState.Info.UserAccountNO), 
                //                                this.DBAccess.CreateDbParameter("@ccode", loginState.Info.EmployeeCode)); 
                //    if (name.ToLower() != "admin" && findRows[0]["cusername"].ToString().ToLower() != "admin") 
                //    { 
                //        var configRow = this.DBCache.Get("sy_config").Select("cparmname='EnableLoginFilter'").First(); 
                //        if (configRow["cparmvalue"].ToString() == "1") 
                //        { 
                //            loginState.State.Success = false; 
                //            loginState.StateCode = 1; 
                //            loginState.State.Message = string.Format("登录失败! 本机机器码【{0}】未审核,请联系管理员。", machinecode); 
                //            return loginState; 
                //        } 
                //    } 
                //} 
            return loginState; 
        } 
        #endregion 

       
    }

} 

#endregion 

