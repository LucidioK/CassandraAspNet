﻿using Cassandra;
using Cassandra.Data.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CreateCassandraDBFromCode
{
    /// <summary>
    ///
    /// </summary>
    /// <example>
    /// var assembly = Assembly.GetAssembly(typeof(StatementEntity));
    /// var connectionString = "Contact Points=localhost;Port=9042";
    /// var ccc = new CassandraTableCreator();
    /// ccc.Initialize(connectionString, "pse", assembly);
    /// ccc.CreateTables();
    /// </example>
    /// <remarks>
    /// Need nuget package CassandraCSharpDriver:
    /// Install-Package CassandraCSharpDriver -Version 3.4.0.1
    /// </remarks>
    public class CassandraTableCreator
    {
        Session session;
        Assembly assembly;
        string keySpaceName;
        Dictionary<string, string> classToTable = new Dictionary<string, string>();
        Dictionary<string, List<string>> tableColumns = new Dictionary<string, List<string>>();
        Dictionary<string, List<string>> tableClusteringKeys = new Dictionary<string, List<string>>();
        Dictionary<string, List<string>> tablePartitionKeys = new Dictionary<string, List<string>>();
        Dictionary<Type, string> CSharpToCassandraTypeEquivalency = new Dictionary<Type, string>()
        {
            { typeof(bool)                , "boolean"       },
            { typeof(decimal)             , "decimal"       },
            { typeof(double)              , "double"        },
            { typeof(float)               , "float"         },
            { typeof(int)                 , "int"           },
            { typeof(long)                , "bigint"        },
            { typeof(string)              , "text"          },
            { typeof(IEnumerable<bool>)   , "list<boolean>" },
            { typeof(IEnumerable<decimal>), "list<decimal>" },
            { typeof(IEnumerable<double>) , "list<double>"  },
            { typeof(IEnumerable<float>)  , "list<float>"   },
            { typeof(IEnumerable<int>)    , "list<int>"     },
            { typeof(IEnumerable<long>)   , "list<bigint>"  },
            { typeof(IEnumerable<string>) , "list<string>"  },
            { typeof(List<bool>)          , "list<boolean>" },
            { typeof(List<decimal>)       , "list<decimal>" },
            { typeof(List<double>)        , "list<double>"  },
            { typeof(List<float>)         , "list<float>"   },
            { typeof(List<int>)           , "list<int>"     },
            { typeof(List<long>)          , "list<bigint>"  },
            { typeof(List<string>)        , "list<string>"  },
        };
        public CassandraTableCreator()
        {

        }

        public void Initialize(string connectionStringOrLocalSettingsJsonFile, string keySpaceName, Assembly assembly)
        {
            this.session = Utils.CassandraUtils.OpenCassandraSession(connectionStringOrLocalSettingsJsonFile);
            this.keySpaceName = keySpaceName;
            this.assembly = assembly;
            try
            {
                this.session.ChangeKeyspace(keySpaceName);
            }
            catch (InvalidQueryException)
            {
                this.session.CreateKeyspaceIfNotExists(keySpaceName);
                this.session.ChangeKeyspace(keySpaceName);
            }
        }

        public void Generate()
        {
            var tableAttribute = typeof(global::Cassandra.Mapping.Attributes.TableAttribute);
            var keySpaceNameLowerCase = this.keySpaceName.ToLowerInvariant();
            var cassandraTypes = this.assembly
                                    .DefinedTypes
                                    .Where(t =>
                                        t.CustomAttributes.Any(a => a.AttributeType == tableAttribute) &&
                                        t.Namespace.ToLowerInvariant().StartsWith(keySpaceNameLowerCase, StringComparison.Ordinal))
                                    .ToList();
            foreach (var cassandraType in cassandraTypes)
            {
                var tableAttributeData = cassandraType
                    .CustomAttributes
                    .FirstOrDefault(a => a.AttributeType == tableAttribute);
                var cassandraTableName =
                    tableAttributeData.ConstructorArguments.Any()
                    ?
                        (string)tableAttributeData.ConstructorArguments[0].Value
                        :
                        Utils.Utils.CassandrifyName(cassandraType.Name);
                System.Console.ForegroundColor = ConsoleColor.Yellow;
                System.Console.WriteLine($"Trying to create table {cassandraTableName} associated with class {cassandraType.Name}");
                System.Console.ResetColor();
                CreateCassandraUserDefinedTypesIfNeeded(cassandraType);
                var tableType = typeof(Table<>).MakeGenericType(cassandraType);
                var tableAttributeValue = (global::Cassandra.Mapping.Attributes.TableAttribute)cassandraType.GetCustomAttribute(tableAttribute);

                var table = Activator.CreateInstance(tableType, new object[] { this.session });
                var createTableIfNotExistsMethod = table
                                    .GetType()
                                    .GetTypeInfo()
                                    .GetMethods()
                                    .FirstOrDefault(m => m.Name.StartsWith("CreateIfNotExists", StringComparison.Ordinal));
                try
                {
                    createTableIfNotExistsMethod.Invoke(table, null);
                    this.classToTable.Add(cassandraType.Name, tableAttributeValue.Name);
                    this.AddTableColumns(cassandraType);
                    this.AddTableClusteringKeys(cassandraType);
                    this.AddPartitionKeys(cassandraType);
                    System.Console.ForegroundColor = ConsoleColor.Green;
                    System.Console.WriteLine($"Created table {cassandraTableName} associated with class {cassandraType.Name}");
                    System.Console.ResetColor();
                }
                catch (Exception e)
                {
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine($"Could not create table {cassandraTableName} associated with class {cassandraType.Name}");
                    System.Console.ResetColor();
                    while (e != null)
                    {
                        System.Console.WriteLine(e.Message);
                        e = e.InnerException;
                    }
                }
            }
        }

        private void CreateCassandraUserDefinedTypesIfNeeded(TypeInfo cassandraType)
        {
            var cassandraColumnPropertiesWithNonValueType = cassandraType
                    .DeclaredProperties
                    .Where(p => IsColumnUsingUserDefinedType(p))
                    .ToList();
            foreach (var cassandraColumnPropertyWithNonValueType in cassandraColumnPropertiesWithNonValueType)
            {
                var associatedPropertyType = GetInnerPropertyType(cassandraColumnPropertyWithNonValueType);
                this.CreateCassandraUserDefinedTypes(associatedPropertyType);
                //var udtMapType     = typeof(UdtMapEx<>).MakeGenericType(associatedPropertyType);
                //var udtMapInstance = Activator.CreateInstance(
                //    udtMapType,
                //    new object[] { this.session, associatedPropertyType.Name });
            }
        }

        private bool IsColumnUsingUserDefinedType(PropertyInfo p)
        {
            var columnAttributeType = typeof(global::Cassandra.Mapping.Attributes.ColumnAttribute);
            var columnAttributeValue = p.CustomAttributes.FirstOrDefault(a => a.AttributeType == columnAttributeType);
            if (columnAttributeValue == null)
            {
                return false;
            }
            var type = GetInnerPropertyType(p);
            return type.IsClass
                    && Utils.Utils.InvariantStartsWith(type.Namespace, this.keySpaceName);
        }

        private static Type GetInnerPropertyType(PropertyInfo p)
        {
            var type = p.PropertyType;
            if (type.GenericTypeArguments != null
                && type.GenericTypeArguments.Any()
                && type.GetInterfaces().Any(i => i.IsAssignableFrom(typeof(IEnumerable<>))))
            {
                type = type.GenericTypeArguments.First();
            }

            return type;
        }

        private void CreateCassandraUserDefinedTypes(Type associatedPropertyType)
        {
            var cassandrifiedTypeName = Utils.Utils.CassandrifyName(associatedPropertyType.Name);
            var cql = $"create type if not exists {cassandrifiedTypeName} (";
            var fieldsDefinition = new StringBuilder();
            foreach (var property in associatedPropertyType.GetProperties())
            {
                if (this.CSharpToCassandraTypeEquivalency.ContainsKey(property.PropertyType))
                {
                    var name = Utils.Utils.CassandrifyName(property.Name);
                    var type = this.CSharpToCassandraTypeEquivalency[property.PropertyType];
                    fieldsDefinition.Append(fieldsDefinition.Length > 0 ? "," : "");
                    fieldsDefinition.Append($"{name} {type}");
                }
            }
            cql += fieldsDefinition + ");";
            this.session.Execute(cql);

            var udtMapType     = typeof(UdtMapEx<>).MakeGenericType(associatedPropertyType);
            var udtMapInstance = Activator.CreateInstance(
                udtMapType,
                new object[] { this.session, cassandrifiedTypeName });
        }

        /*
           session.UserDefinedTypes.Define(
           UdtMap.For<Address>()
              .Map(a => a.Street, "street")
              .Map(a => a.City, "city")
              .Map(a => a.Zip, "zip")
              .Map(a => a.Phones, "phones")
        );
         */
        private void AddPartitionKeys(TypeInfo cassandraType)
        {
            AddToColumnList(
                            cassandraType,
                            typeof(global::Cassandra.Mapping.Attributes.PartitionKeyAttribute),
                            this.tablePartitionKeys);
        }

        private void AddTableClusteringKeys(TypeInfo cassandraType)
        {
            AddToColumnList(
                            cassandraType,
                            typeof(global::Cassandra.Mapping.Attributes.ClusteringKeyAttribute),
                            this.tableClusteringKeys);
        }

        private void AddTableColumns(TypeInfo cassandraType)
        {
            AddToColumnList(
                            cassandraType,
                            typeof(global::Cassandra.Mapping.Attributes.ColumnAttribute),
                            this.tableColumns);
        }

        private static void AddToColumnList(TypeInfo cassandraType, Type type, Dictionary<string, List<string>> stringToListDictionary)
        {
            stringToListDictionary.TryAdd(cassandraType.Name, new List<string>());

            var properties = cassandraType
                .DeclaredProperties
                .Where(p => p.CustomAttributes.Any(a => a.AttributeType == type))
                .ToList();
            properties
                .ForEach(p => stringToListDictionary[cassandraType.Name].Add(p.Name));
        }


    }

    class UdtMapEx<T> : UdtMap where T:new()
    {
        public UdtMapEx(Session session, string cassandrifiedUdtName) : base(typeof(T), cassandrifiedUdtName)
        {
            var udtMap = UdtMap.For<T>(cassandrifiedUdtName);
            session.UserDefinedTypes.Define(udtMap);
        }
    }
}
