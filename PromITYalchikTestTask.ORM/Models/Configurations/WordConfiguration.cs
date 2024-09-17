using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PromITYalchikTestTask.SqlClient.Models.Configurations
{
    public class WordConfiguration : IEntityTypeConfiguration<WordEntity>
    {
        public void Configure(EntityTypeBuilder<WordEntity> builder)
        {
            builder.HasKey(x => x.Id);

            builder
                .Property(w => w.Id)
                .HasMaxLength(36)
                .HasDefaultValueSql("NEWID()")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder
                .Property(w => w.Word)
                .HasMaxLength(20)
                .IsRequired();

            builder
                .HasIndex(x => x.Word)
                .IsUnique();
                
            builder
                .Property(w => w.Count)
                .HasDefaultValue(0)
                .IsRequired();
        }
    }
}
