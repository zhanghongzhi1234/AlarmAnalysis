using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.ComponentModel;

namespace TemplateProject
{
    public partial class testWin1 : Window
    {
        ListCollectionView lcv;
        ListSortDirection direction = ListSortDirection.Ascending;
        int maxIndex = 0;

        public testWin1()
        {
            InitializeComponent();

            List<Item> items = new List<Item>();
            items.Add(new Item() { Name = "Item1", Category = "A" });

            items.Add(new Item() { Name = "Item5", Category = "B" });
            items.Add(new Item() { Name = "Item3", Category = "A" });
            items.Add(new Item() { Name = "Item4", Category = "B" });
            items.Add(new Item() { Name = "Item2", Category = "A" });
            maxIndex = 5;
            lcv = new ListCollectionView(items);
            lcv.GroupDescriptions.Add(new PropertyGroupDescription("Category"));

            this.DataGrid.ItemsSource = lcv;
        }

        public class Item
        {
            public string Name { get; set; }
            public string Category { get; set; }
        }

        private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;
            if (direction == ListSortDirection.Ascending)
            {
                direction = ListSortDirection.Descending;
            }
            else
            {
                direction = ListSortDirection.Ascending;
            }
            lcv.SortDescriptions.Clear();
            lcv.SortDescriptions.Add(new SortDescription(e.Column.SortMemberPath, direction));
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            lcv.AddNewItem(new Item() { Name = "Item" + (maxIndex++), Category = "A" });
            lcv.CommitNew();
        }
    }
}