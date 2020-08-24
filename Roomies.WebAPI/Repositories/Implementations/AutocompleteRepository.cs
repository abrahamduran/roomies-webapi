using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Repositories.Interfaces;

namespace Roomies.WebAPI.Repositories.Implementations
{
    public class AutocompleteRepository : IAutocompleteRepository
    {
        private const string COLLECTION_NAME = "autocomplete";

        private readonly IMongoCollection<Autocomplete> _autocomplete;

        public AutocompleteRepository(MongoDBContext context)
        {
            _autocomplete = context.database.GetCollection<Autocomplete>(COLLECTION_NAME);

            #region Create Indices
            _autocomplete.CreateIndex(x => x.Type);
            _autocomplete.CreateTextIndex(x => x.Text);
            #endregion
        }

        public void Index(IEnumerable<Autocomplete> autocomplete)
        {
            foreach (var item in autocomplete)
            {
                var filter = Builders<Autocomplete>.Filter.Eq(x => x.Text, item.Text);
                var docExists = _autocomplete.Find(filter).CountDocuments() > 0;
                if (!docExists)
                    _autocomplete.InsertOne(item);
            }
        }

        public IEnumerable<string> Search(string text, AutocompleteType? type)
        {
            var searchText = Builders<Autocomplete>.Filter.Text(text);
            if (type != null)
                searchText = searchText & Builders<Autocomplete>.Filter.Eq(x => x.Type, type);
            var projection = Builders<Autocomplete>.Projection.MetaTextScore("textScore").Include(x => x.Text).Exclude(x => x.Id);
            var sorting = Builders<Autocomplete>.Sort.MetaTextScore("textScore");

            return _autocomplete.Find(searchText).Project<Autocomplete>(projection).Sort(sorting).ToList().Select(x => x.Text);
        }
    }
}
