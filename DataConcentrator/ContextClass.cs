using System.Data.Entity;
using DataConcentrator.Model;

namespace DataConcentrator
{
    public class ContextClass : DbContext
    {
        static ContextClass()
        {
            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<ContextClass>());
        }

        private static ContextClass instance;

        public static ContextClass Instance
        {
            get
            {
                if (instance == null)
                    instance = new ContextClass();
                return instance;
            }
        }

        public static ContextClass CreateNew() => new ContextClass();

        public DbSet<Tag> Tags { get; set; }
        public DbSet<Alarm> Alarms { get; set; }
        public DbSet<ActivatedAlarm> ActivatedAlarms { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AnalogInput>().HasMany(a => a.Alarms).WithRequired(a => a.Tag).HasForeignKey(a => a.TagName);
        }
    }
}

