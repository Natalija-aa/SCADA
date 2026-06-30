using System.Data.Entity;
using DataConcentrator.Model;

namespace DataConcentrator
{
    public class ContextClass : DbContext

    {   // DropCreateDatabaseIfModelChanges - ako se C# klase promene brise se stara baza i pravi novu
        static ContextClass()
        {
            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<ContextClass>());
        }

        private static ContextClass instance;   // globalna instanca

        public static ContextClass Instance
        {
            get
            {
                if (instance == null)
                    instance = new ContextClass();
                return instance;
            }
        }

        // nova konekcija sa bazom - u Checkalarms jer kesira stare vrednosti alarma
        public static ContextClass CreateNew() => new ContextClass();

        public DbSet<Tag> Tags { get; set; }
        public DbSet<Alarm> Alarms { get; set; }
        public DbSet<ActivatedAlarm> ActivatedAlarms { get; set; }

        // analogni input - vise alarma
        // veza Alarm/TagName - Tag.Name
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AnalogInput>().HasMany(a => a.Alarms).WithRequired(a => a.Tag).HasForeignKey(a => a.TagName);
        }
    }
}

