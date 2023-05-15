﻿#define USE_ASYNC

using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;

public partial class AssetsPage : System.Web.UI.Page
{
    MySqlSvr svr;
    bool loggedIn= false;
    string userId;

    protected async void Page_Load(object sender, EventArgs e)
    {
        Page.MaintainScrollPositionOnPostBack = true;
        if (!loggedIn && Session["UserID"] != null)
        {
            loggedIn= true;
            userId = (string)Session["UserID"];
        }
        if (IsPostBack) { return; }
        svr = new MySqlSvr(new MySqlConnection("server=127.0.0.1; database=nuedc; user id=notRoot; password=1234"));
        //Update system usage public summary
#if USE_ASYNC
        svr.cn.Open();
        var tc = svr.cmd("select sum(Amount) from assets group by com;").ExecuteScalarAsync();
        var te = svr.cmd("select count(AssetCode) from assets group by com;").ExecuteScalarAsync();
        var tl = svr.cmd("select sum(Qty) from lending group by com;").ExecuteScalarAsync();
        var tr = svr.cmd("select sum(Qty) from lending group by TransactionCycleEnded having TransactionCycleEnded=1;").ExecuteScalarAsync();

        TotalComp_lb.Text = (await tc)?.ToString();
        TotalEntries_lb.Text = (await te)?.ToString();
        TotalLent_lb.Text = (await tl)?.ToString();
        TotalReg_lb.Text = (await tr)?.ToString();
#else //USE_ASYNC
        TotalComp_lb.Text = (string)svr.QuerySingle("select sum(Amount) from assets group by com;");
        TotalEntries_lb.Text = (string)svr.QuerySingle("select count(AssetCode) from assets group by com;");
        TotalLent_lb.Text = (string)svr.QuerySingle("select sum(Qty) from lending group by com;");
        TotalReturned_lb.Text = (string)svr.QuerySingle("select sum(Qty) from lending group by TransactionCycleEnded having TransactionCycleEnded=1;");
        //TotalQueries_lb.Text = (string) svr.QuerySingle("select ")
#endif //USE_ASYNC
        InitiateSearch("select AssetCode, AssetName, MainValue, ValueUnit from assets left join assetclasses on assets.ClassCode = assetclasses.ClassCode;");
        DynamicControls.CreateAlert("This is a debug version of the website. Content may change at any time.", "notice", Alerts_pn);
        //DynamicControls.CreateAlert("Info", "info", Alerts_pn);
        LoadAssetData($"assets.AssetCode='{Asset_gv.Rows[0].Cells[1].Text}'");
    }

    private void InitiateSearch(string sql)
    {
        Asset_gv.DataSource = svr.QueryDataset(sql);
        Asset_gv.DataBind();
    }

    protected void BorrowQtySel_tb_TextChanged(object sender, EventArgs e)
    {
        //if(int.Parse(BorrowQtySel_tb.Text) > int.Parse(Borrowable_lb.Text))
        if (int.Parse(BorrowQtySel_tb.Text) > 16)
        {
            //BorrowQtySel_tb.Text = Borrowable_lb.Text;
            BorrowQtySel_tb.Text = "16";
            return;
        }
        if(int.Parse(BorrowQtySel_tb.Text) < 0)
        {
            BorrowQtySel_tb.Text = "0";
            return;
        }
    }

    public void LoadAssetData(string sql_where)
    {
        svr.QueryReader($"select AssetName, ClassName, MainValue, ValueUnit, Location, Characteristics, Datasheet, Amount-ReservationQty-(select count(TransactionCode) from lending right join assets on lending.AssetCode=assets.AssetCode where {sql_where} and Status='Returned') from assets left join assetclasses on assets.ClassCode=assetclasses.ClassCode where {sql_where};", 
            AssignAssetData,
            () => Alert_DBQueryEmpty_pn.Visible = true);
    }

    private void AssignAssetData(MySqlDataReader rd)
    {
        AssetName_lb.Text   = (string)rd[0];
        AssetClass_lb.Text  = (string)rd[1];
        PrimValue_lb.Text   = $"{rd[2]} {rd[3]}";
        Location_lb.Text    = (string)rd[4];
        Property_lb.Text = rd[5].GetType() == Type.GetType("DBNull") ? (string)rd[5] : "暂无数据";
        if (((string)rd[6]).Length == 0)
        {
            Datasheet_lk.Text = "暂无";
            Datasheet_lk.NavigateUrl = "#0";
        }
        else
        {
            Datasheet_lk.Text = "点击访问";
            Datasheet_lk.NavigateUrl = (string)rd[6];
        }
        Borrowable_lb.Text = rd[7]?.ToString();
        if ((UInt64)rd[7] > 0)
        {
            BorrowQtySel_tb.Enabled = true;
            BorrowConfirm_pn.Visible = true;
            BorrowNotAvailable_pn.Visible = false;
        }
        else
        {
            BorrowQtySel_tb.Enabled = false;
            BorrowConfirm_pn.Visible = false;
            BorrowNotAvailable_pn.Visible = true;
        }
    }
}