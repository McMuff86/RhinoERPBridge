using System;
using System.Runtime.InteropServices;
using Eto.Forms;
using Eto.Drawing;
using System.Collections.Generic;
using System.Linq;
using RhinoERPBridge.Data;
using RhinoERPBridge.Models;

namespace RhinoERPBridge.UI
{
    [Guid("6A6A74A3-6F6C-4F2D-89C1-7A0A3F3C7E5F")]
    public class ErpSearchPanel : Panel
    {
        public static Guid PanelGuid => typeof(ErpSearchPanel).GUID;

        private readonly TextBox _searchBox;
        private readonly Button _searchButton;
        private readonly Label _statusLabel;
        private readonly GridView _grid;
        private readonly CheckBox _showAllColumns;
        private IArticleRepository _repo;
        private readonly Button _settingsButton;
        private readonly Button _normalViewButton;

        public ErpSearchPanel()
        {
            var layout = new DynamicLayout { Spacing = new Size(6, 6), Padding = new Padding(8) };

            layout.AddRow(new Label { Text = "ERP Article Search", Font = SystemFonts.Bold(SystemFonts.Default().Size + 1) });

            _searchBox = new TextBox { PlaceholderText = "Search term..." };
            _searchButton = new Button { Text = "Search" };
            _statusLabel = new Label { Text = "Ready", TextColor = Colors.Gray };
            _settingsButton = new Button { Text = "DB Settings..." };
            _showAllColumns = new CheckBox { Text = "Show all columns" };
            _normalViewButton = new Button { Text = "Normal view" };

            _grid = BuildGrid();

            _searchButton.Click += (s, e) => ApplySearch();
            _searchBox.KeyDown += (s, e) => { if (e.Key == Keys.Enter) ApplySearch(); };

            // Context menu for copy actions
            var miCopyValue = new ButtonMenuItem { Text = "Copy value" };
            miCopyValue.Click += (s, e) => CopySelectedValue();
            var miCopyRow = new ButtonMenuItem { Text = "Copy row" };
            miCopyRow.Click += (s, e) => CopySelectedRow();
            _grid.ContextMenu = new ContextMenu(miCopyValue, miCopyRow);

            // Search term on its own row (full width)
            layout.Add(_searchBox, xscale: true);
            // Action buttons in one row sharing the width (use nested TableLayout so main layout stays single-column)
            var buttonsRow = new TableLayout(
                new TableRow(
                    new TableCell(_searchButton, true),
                    new TableCell(_settingsButton, true),
                    new TableCell(_normalViewButton, true)
                )
            );
            layout.AddRow(buttonsRow);

            layout.AddRow(_statusLabel);
            layout.AddRow(_showAllColumns);
            layout.Add(_grid, xscale: true, yscale: true);

            Content = layout;

            // repo selection
            _repo = CreateRepository();
            try
            {
                BindArticles(_repo.GetAll());
            }
            catch (Exception ex)
            {
                _statusLabel.Text = ex.Message;
                _statusLabel.TextColor = Colors.IndianRed;
            }

            _settingsButton.Click += (s, e) => Rhino.UI.Panels.OpenPanel(DbSettingsPanel.PanelGuid);
            _showAllColumns.CheckedChanged += (s, e) => ApplySearch();
            _normalViewButton.Click += (s, e) => { _showAllColumns.Checked = false; ApplySearch(); };
        }

        private IArticleRepository CreateRepository()
        {
            var settings = RhinoERPBridge.Services.SettingsService.Load();
            if (settings != null && settings.IsConfigured && !string.IsNullOrWhiteSpace(settings.Database))
            {
                try
                {
                    return new RhinoERPBridge.Data.SqlArticleRepository(settings);
                }
                catch
                {
                    // fall back to JSON if construction fails
                }
            }
            return new JsonArticleRepository();
        }

        private GridView BuildGrid()
        {
            var grid = new GridView
            {
                ShowHeader = true,
                AllowColumnReordering = true
            };

            grid.Columns.Add(new GridColumn { HeaderText = "SKU", DataCell = new TextBoxCell { Binding = Binding.Property<Article, string>(a => a.Sku) }, Width = 120 });
            grid.Columns.Add(new GridColumn { HeaderText = "Name", DataCell = new TextBoxCell { Binding = Binding.Property<Article, string>(a => a.Name) }, Width = 220 });
            grid.Columns.Add(new GridColumn { HeaderText = "Category", DataCell = new TextBoxCell { Binding = Binding.Property<Article, string>(a => a.Category) }, Width = 120 });
            grid.Columns.Add(new GridColumn { HeaderText = "Price", DataCell = new TextBoxCell { Binding = Binding.Property<Article, string>(a => a.Price.ToString("F2")) }, Width = 80 });
            grid.Columns.Add(new GridColumn { HeaderText = "Stock", DataCell = new TextBoxCell { Binding = Binding.Property<Article, string>(a => a.Stock.ToString()) }, Width = 70 });

            return grid;
        }

        private void ApplySearch()
        {
            var term = _searchBox.Text ?? string.Empty;
            if (_showAllColumns.Checked == true)
            {
                TryBindDynamic(term);
                return;
            }
            var results = _repo.Search(term);
            _statusLabel.Text = $"{results.Count} results";
            BindArticles(results);
        }

        private void TryBindDynamic(string term)
        {
            try
            {
                var settings = RhinoERPBridge.Services.SettingsService.Load();
                if (settings == null || !settings.IsConfigured || string.IsNullOrWhiteSpace(settings.ArticlesTable))
                {
                    _statusLabel.Text = "DB Settings not configured";
                    _statusLabel.TextColor = Colors.IndianRed;
                    return;
                }

                var data = RhinoERPBridge.Data.DynamicSqlHelper.QueryTop(settings, term);
                BuildColumnsForDynamic(data);
                _grid.DataStore = data.Rows;
                _statusLabel.Text = $"{data.Rows.Count} rows (dynamic)";
                _statusLabel.TextColor = Colors.Gray;
            }
            catch (System.Exception ex)
            {
                _statusLabel.Text = ex.Message;
                _statusLabel.TextColor = Colors.IndianRed;
            }
        }

        private void BuildColumnsForDynamic(RhinoERPBridge.Data.DynamicResult data)
        {
            _grid.Columns.Clear();
            foreach (var col in data.Columns)
            {
                var column = new GridColumn
                {
                    HeaderText = col,
                    DataCell = new TextBoxCell { Binding = GetDictBinding(col) }
                };
                // auto width based on header length (approximate)
                var width = Math.Max(80, Math.Min(400, (col?.Length ?? 0) * 8 + 24));
                column.Width = width;
                _grid.Columns.Add(column);
            }
        }

        private static IIndirectBinding<string> GetDictBinding(string column)
        {
            return Binding.Delegate<System.Collections.Generic.Dictionary<string, object>, string>(row =>
            {
                object value;
                if (row != null && row.TryGetValue(column, out value) && value != null)
                    return value.ToString();
                return null;
            }, null);
        }

        private void BindArticles(IReadOnlyList<Article> items)
        {
            _grid.DataStore = items;
        }

        private void CopySelectedValue()
        {
            var text = GetSelectedValueText();
            if (!string.IsNullOrEmpty(text))
            {
                var cb = new Clipboard();
                cb.Text = text;
            }
        }

        private void CopySelectedRow()
        {
            var text = GetSelectedRowText();
            if (!string.IsNullOrEmpty(text))
            {
                var cb = new Clipboard();
                cb.Text = text;
            }
        }

        private string GetSelectedValueText()
        {
            var item = _grid.SelectedItem;
            if (item == null) return null;
            if (item is Article a)
                return !string.IsNullOrWhiteSpace(a.Name) ? a.Name : a.Sku;
            if (item is Dictionary<string, object> d)
            {
                var firstCol = _grid.Columns.Count > 0 ? _grid.Columns[0].HeaderText : null;
                if (!string.IsNullOrEmpty(firstCol) && d.TryGetValue(firstCol, out var v) && v != null)
                    return v.ToString();
                var kv = d.FirstOrDefault();
                return kv.Value?.ToString();
            }
            return item.ToString();
        }

        private string GetSelectedRowText()
        {
            var item = _grid.SelectedItem;
            if (item == null) return null;
            if (item is Article a)
            {
                var parts = new[]
                {
                    a.Sku,
                    a.Name,
                    a.Category,
                    a.Price.ToString("F2"),
                    a.Stock.ToString()
                };
                return string.Join("\t", parts);
            }
            if (item is Dictionary<string, object> d)
            {
                var values = _grid.Columns.Select(c =>
                {
                    if (d.TryGetValue(c.HeaderText, out var v) && v != null) return v.ToString();
                    return string.Empty;
                });
                return string.Join("\t", values);
            }
            return item.ToString();
        }
    }
}


