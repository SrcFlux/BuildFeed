﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Required = System.ComponentModel.DataAnnotations.RequiredAttribute;

namespace BuildFeed.Models
{
   [DataObject]
   public class MetaItemModel
   {
      [Key]
      [BsonId]
      [@Required]
      public MetaItemKey Id { get; set; }

      [DisplayName("Page Content")]
      [AllowHtml]
      public string PageContent { get; set; }

      [DisplayName("Meta Description")]
      public string MetaDescription { get; set; }
   }

   public class MetaItem
   {
      private const string _metaCollectionName = "metaitem";

      private MongoClient _dbClient;
      private IMongoCollection<MetaItemModel> _metaCollection;
      private Build bModel;

      public MetaItem()
      {
         _dbClient = new MongoClient(new MongoClientSettings()
         {
            Server = new MongoServerAddress(MongoConfig.Host, MongoConfig.Port)
         });

         _metaCollection = _dbClient.GetDatabase(MongoConfig.Database).GetCollection<MetaItemModel>(_metaCollectionName);
         bModel = new Build();
      }

      [DataObjectMethod(DataObjectMethodType.Select, false)]
      public async Task<IEnumerable<MetaItemModel>> Select()
      {
         return await _metaCollection
            .Find(new BsonDocument())
            .ToListAsync();
      }

      [DataObjectMethod(DataObjectMethodType.Select, true)]
      public async Task<IEnumerable<MetaItemModel>> SelectByType(MetaType type)
      {
         return await _metaCollection
            .Find(f => f.Id.Type == type)
            .ToListAsync();
      }

      [DataObjectMethod(DataObjectMethodType.Select, false)]
      public async Task<MetaItemModel> SelectById(MetaItemKey id)
      {
         return await _metaCollection
            .Find(f => f.Id.Type == id.Type && f.Id.Value == id.Value)
            .SingleOrDefaultAsync();
      }

      [DataObjectMethod(DataObjectMethodType.Select, false)]
      public async Task<IEnumerable<string>> SelectUnusedLabs()
      {
         var labs = await bModel.SelectLabs();

         var usedLabs = await _metaCollection.Find(f => f.Id.Type == MetaType.Lab).ToListAsync();

         return from l in labs
                where usedLabs.All(ul => ul.Id.Value != l)
                select l;
      }

      [DataObjectMethod(DataObjectMethodType.Select, false)]
      public async Task<IEnumerable<string>> SelectUnusedVersions()
      {
         var versions = await bModel.SelectVersions();

         var usedVersions = await _metaCollection.Find(f => f.Id.Type == MetaType.Version).ToListAsync();

         return from v in versions
                where usedVersions.All(ul => ul.Id.Value != v.ToString())
                select v.ToString();
      }

      [DataObjectMethod(DataObjectMethodType.Select, false)]
      public async Task<IEnumerable<string>> SelectUnusedYears()
      {
         var years = await bModel.SelectYears();

         var usedYears = await _metaCollection.Find(f => f.Id.Type == MetaType.Year).ToListAsync();

         return from y in years
                where usedYears.All(ul => ul.Id.Value != y.ToString())
                select y.ToString();
      }

      [DataObjectMethod(DataObjectMethodType.Insert, true)]
      public async Task Insert(MetaItemModel item)
      {
         await _metaCollection
            .InsertOneAsync(item);
      }

      [DataObjectMethod(DataObjectMethodType.Update, true)]
      public async Task Update(MetaItemModel item)
      {
         await _metaCollection
            .ReplaceOneAsync(f => f.Id.Type == item.Id.Type && f.Id.Value == item.Id.Value, item);
      }

      [DataObjectMethod(DataObjectMethodType.Insert, false)]
      public async Task InsertAll(IEnumerable<MetaItemModel> items)
      {
         await _metaCollection
            .InsertManyAsync(items);
      }

      [DataObjectMethod(DataObjectMethodType.Delete, true)]
      public async Task DeleteById(MetaItemKey id)
      {
         await _metaCollection
            .DeleteOneAsync(f => f.Id.Type == id.Type && f.Id.Value == id.Value);
      }
   }

   public class MetaItemKey
   {
      public string Value { get; set; }
      public MetaType Type { get; set; }

      public MetaItemKey()
      {

      }

      public MetaItemKey(string id)
      {
         var items = id.Split(':');
         Type = (MetaType)Enum.Parse(typeof(MetaType), items[0]);
         Value = items[1];
      }

      public override string ToString()
      {
         return $"{Type}:{Value}";
      }
   }

   public enum MetaType
   {
      Lab,
      Version,
      Source,
      Year
   }
}