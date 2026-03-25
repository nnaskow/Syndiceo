using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Syndiceo.Data.Models;

namespace Syndiceo.Windows
{
    public partial class SearchWindow : Window
    {
        public SearchWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            string searchText = Properties.Settings.Default.currentSearch;
            SearchResultsLabel.Text = "Намерени резултати за: " + searchText;

            var results = GetSearchResults(searchText);

            if (results == null || results.Count == 0)
            {
                MessageBox.Show("Няма намерени резултати за търсенето.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
                return;
            }

            SearchResultsDataGrid.ItemsSource = results;
        }    

        private List<SearchResult> GetSearchResults(string query)
        {
            using var context = new SyndiceoDBContext();

            bool isNumber = query.All(char.IsDigit);

            var list = context.Owners
                              .Where(o => isNumber
                                          ? o.PhoneNumber.Contains(query)
                                          : o.OwnerName.Contains(query)
                                            || o.Apartment.Entrance.EntranceName.Contains(query)
                                            || o.Apartment.Entrance.Block.BlockName.Contains(query))
                              .Select(o => new SearchResult
                              {
                                  OwnerName = o.OwnerName,
                                  Phone = o.PhoneNumber,
                                  FullAddress = "ул."+o.Apartment.Entrance.Block.Address.Street + ", " + o.Apartment.Entrance.Block.BlockName + ", вх." + o.Apartment.Entrance.EntranceName + ", апт." + o.Apartment.ApartmentNumber
                              }).ToList();

            return list;
        }
    }

    public class SearchResult
    {
        public string OwnerName { get; set; }
        public string Phone { get; set; }
        public string ApartmentNumber { get; set; }
        public string EntranceName { get; set; }
        public string BlockName { get; set; }
        public string FullAddress { get; set; }
    }
}
