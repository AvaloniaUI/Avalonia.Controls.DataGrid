using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Threading;
using DataGridSample.Models;

namespace DataGridSample
{
    public partial class DataGridPage : UserControl
    {
        public DataGridPage()
        {
            this.InitializeComponent();

            var dataGridSortDescription = DataGridSortDescription.FromPath(nameof(Country.Region), ListSortDirection.Ascending, new ReversedStringComparer());
            var collectionView1 = new DataGridCollectionView(Countries.All);
            collectionView1.SortDescriptions.Add(dataGridSortDescription);
            var dg1 = this.Get<DataGrid>("dataGrid1");
            dg1.IsReadOnly = true;
            dg1.Sorting += (s, a) =>
            {
                var binding = (a.Column as DataGridBoundColumn)?.Binding as Binding;

                if (binding?.Path is string property
                    && property == dataGridSortDescription.PropertyPath
                    && !collectionView1.SortDescriptions.Contains(dataGridSortDescription))
                {
                    collectionView1.SortDescriptions.Add(dataGridSortDescription);
                }
            };
            dg1.ItemsSource = collectionView1;

            var dg2 = this.Get<DataGrid>("dataGridGrouping");
            dg2.IsReadOnly = true;

            var collectionView2 = new DataGridCollectionView(Countries.All);
            collectionView2.GroupDescriptions.Add(new DataGridPathGroupDescription("Region"));

            dg2.ItemsSource = collectionView2;

            var dg3 = this.Get<DataGrid>("dataGridEdit");
            dg3.IsReadOnly = false;

            var list = new ObservableCollection<Person>
            {
                new Person { FirstName = "John", LastName = "Doe" , Age = 30},
                new Person { FirstName = "Elizabeth", LastName = "Thomas", IsBanned = true , Age = 40 },
                new Person { FirstName = "Zack", LastName = "Ward" , Age = 50 }
            };
            DataGrid3Source = list;

            var addButton = this.Get<Button>("btnAdd");
            addButton.Click += (a, b) => list.Add(new Person());

            // Collection Mutations tab
            var mutationCounter = 0;
            var mutationList = new AvaloniaList<Person>
            {
                new Person { FirstName = "Alice", LastName = "Smith", Age = 25 },
                new Person { FirstName = "Bob", LastName = "Jones", Age = 30 },
                new Person { FirstName = "Carol", LastName = "White", Age = 35 },
                new Person { FirstName = "Dave", LastName = "Brown", Age = 40 },
                new Person { FirstName = "Eve", LastName = "Davis", Age = 45 },
            };

            MutationsSource = mutationList;

            var dgMutations = this.Get<DataGrid>("dataGridMutations");

            var btnMoveUp = this.Get<Button>("btnMutMoveUp");
            var btnMoveDown = this.Get<Button>("btnMutMoveDown");
            btnMoveUp.IsEnabled = false;
            btnMoveDown.IsEnabled = false;
            dgMutations.SelectionChanged += (s, e) =>
            {
                var (firstIndex, count) = GetContiguousSelectionRange(dgMutations, mutationList);
                btnMoveUp.IsEnabled = firstIndex > 0;
                btnMoveDown.IsEnabled = firstIndex >= 0 && firstIndex + count < mutationList.Count;
            };

            this.Get<Button>("btnMutAdd").Click += (s, e) =>
            {
                mutationCounter++;
                mutationList.Add(new Person
                {
                    FirstName = $"New{mutationCounter}",
                    LastName = $"Person{mutationCounter}",
                    Age = 20 + mutationCounter
                });
            };

            this.Get<Button>("btnMutAddRange").Click += (s, e) =>
            {
                mutationList.AddRange(Enumerable.Range(0, 3).Select(_ =>
                {
                    mutationCounter++;
                    return new Person
                    {
                        FirstName = $"New{mutationCounter}",
                        LastName = $"Person{mutationCounter}",
                        Age = 20 + mutationCounter
                    };
                }));
            };

            this.Get<Button>("btnMutRemove").Click += (s, e) =>
            {
                var selected = dgMutations.SelectedItems.Cast<Person>().ToList();
                mutationList.RemoveAll(selected);
            };

            this.Get<Button>("btnMutMoveUp").Click += (s, e) =>
            {
                var (firstIndex, count) = GetContiguousSelectionRange(dgMutations, mutationList);
                if (firstIndex > 0)
                {
                    var selected = dgMutations.SelectedItems.Cast<Person>().ToList();
                    mutationList.MoveRange(firstIndex, count, firstIndex - 1);
                    dgMutations.SelectedItems.Clear();
                    foreach (var item in selected)
                    {
                        dgMutations.SelectedItems.Add(item);
                    }
                }
            };

            this.Get<Button>("btnMutMoveDown").Click += (s, e) =>
            {
                var (firstIndex, count) = GetContiguousSelectionRange(dgMutations, mutationList);
                if (firstIndex >= 0 && firstIndex + count < mutationList.Count)
                {
                    var selected = dgMutations.SelectedItems.Cast<Person>().ToList();
                    mutationList.MoveRange(firstIndex, count, firstIndex + count);
                    dgMutations.SelectedItems.Clear();
                    foreach (var item in selected)
                    {
                        dgMutations.SelectedItems.Add(item);
                    }
                }
            };

            var btnMoveInPlace = this.Get<Button>("btnMutMoveInPlace");
            btnMoveInPlace.IsEnabled = false;
            dgMutations.SelectionChanged += (s, e) =>
            {
                var (idx, cnt) = GetContiguousSelectionRange(dgMutations, mutationList);
                btnMoveInPlace.IsEnabled = idx >= 0;
            };
            btnMoveInPlace.Click += (s, e) =>
            {
                var (firstIndex, count) = GetContiguousSelectionRange(dgMutations, mutationList);
                if (firstIndex >= 0)
                {
                    mutationList.MoveRange(firstIndex, count, firstIndex);
                }
            };

            DataContext = this;
        }

        public IEnumerable<Person> DataGrid3Source { get; }

        public IEnumerable<Person> MutationsSource { get; }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private static (int firstIndex, int count) GetContiguousSelectionRange(DataGrid grid, IList<Person> list)
        {
            var indices = grid.SelectedItems.Cast<Person>().Select(list.IndexOf).OrderBy(i => i).ToList();
            if (indices.Count == 0)
            {
                return (-1, 0);
            }

            for (var i = 1; i < indices.Count; i++)
            {
                if (indices[i] != indices[i - 1] + 1)
                {
                    return (-1, 0);
                }
            }

            return (indices[0], indices.Count);
        }

        private class ReversedStringComparer : IComparer<object>, IComparer
        {
            public int Compare(object? x, object? y)
            {
                if (x is string left && y is string right)
                {
                    var reversedLeft = new string(left.Reverse().ToArray());
                    var reversedRight = new string(right.Reverse().ToArray());
                    return reversedLeft.CompareTo(reversedRight);
                }

                return Comparer.Default.Compare(x, y);
            }
        }

        private void NumericUpDown_OnTemplateApplied(object sender, TemplateAppliedEventArgs e)
        {
            // We want to focus the TextBox of the NumericUpDown. To do so we search for this control when the template
            // is applied, but we postpone the action until the control is actually loaded. 
            if (e.NameScope.Find<TextBox>("PART_TextBox") is {} textBox)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }, DispatcherPriority.Loaded);
            }
        }
    }
}
