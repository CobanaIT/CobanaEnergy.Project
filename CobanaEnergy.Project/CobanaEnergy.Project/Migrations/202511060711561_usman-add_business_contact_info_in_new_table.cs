namespace CobanaEnergy.Project.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class usmanadd_business_contact_info_in_new_table : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CE_BusinessContactInfo",
                c => new
                    {
                        EId = c.String(nullable: false, maxLength: 128),
                        Type = c.String(),
                        BusinessName = c.String(maxLength: 300),
                        CustomerName = c.String(maxLength: 300),
                        PhoneNumber1 = c.String(),
                        PhoneNumber2 = c.String(),
                        EmailAddress = c.String(),
                    })
                .PrimaryKey(t => t.EId)
                .Index(t => t.BusinessName)
                .Index(t => t.CustomerName);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.CE_BusinessContactInfo", new[] { "CustomerName" });
            DropIndex("dbo.CE_BusinessContactInfo", new[] { "BusinessName" });
            DropTable("dbo.CE_BusinessContactInfo");
        }
    }
}
