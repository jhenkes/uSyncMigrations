﻿using System.Reflection.PortableExecutable;

using Umbraco.Extensions;

using uSync.Core.Serialization;
using uSync.Migrations.Extensions;
using uSync.Migrations.Models;

namespace uSync.Migrations.Migrators;

public abstract class SyncPropertyMigratorBase : ISyncPropertyMigrator
{
    private readonly Type? configurationType; 

    protected readonly string _defaultAlias;

    public virtual string[] Editors { get; private set; }

    protected SyncPropertyMigratorBase()
    {
        // read the attribute
        var attributes = this.GetType().GetCustomAttributes<SyncMigratorAttribute>(false);
        if (attributes != null && attributes.Any())
        {
            Editors = attributes.Select(x => x.EditorAlias).ToArray();

            if (attributes.Count(x => x.ConfigurationType != null) > 1)
            {
                throw new InvalidOperationException("Cannot have multiple configuration types via SyncMigratorAttribute");
            }
            configurationType = attributes.FirstOrDefault(x => x.ConfigurationType != null)?.ConfigurationType ?? null;

            if (attributes.Count(x => x.IsDefaultAlias) > 1)
            {
                throw new InvalidOperationException("Cannot have multiple default editor attributes");
            }

            var defaultAttribute = attributes.FirstOrDefault(x => x.IsDefaultAlias);
            if (defaultAttribute != null)
            {
                _defaultAlias = defaultAttribute.EditorAlias;
            }
        }
        else
        {
            throw new InvalidOperationException($"Migrators inheriting from {nameof(SyncPropertyMigratorBase)} must contain at least one ${nameof(SyncMigratorAttribute)}");
        }
    }



    public virtual string GetDatabaseType(SyncMigrationDataTypeProperty dataTypeProperty, SyncMigrationContext context)
        => dataTypeProperty.DatabaseType;

    public virtual string GetEditorAlias(SyncMigrationDataTypeProperty dataTypeProperty, SyncMigrationContext context)
        => string.IsNullOrWhiteSpace(_defaultAlias) ? dataTypeProperty.EditorAlias : _defaultAlias;

    public virtual object GetConfigValues(SyncMigrationDataTypeProperty dataTypeProperty, SyncMigrationContext context)
    {
        if (configurationType is not null) 
        { 
            return Activator.CreateInstance(configurationType).MapPreValues(dataTypeProperty.PreValues);
        }

        return dataTypeProperty.PreValues.ConvertPreValuesToJson(false);
    }

    public virtual string GetContentValue(SyncMigrationContentProperty contentProperty, SyncMigrationContext context)
        => contentProperty.Value;
}