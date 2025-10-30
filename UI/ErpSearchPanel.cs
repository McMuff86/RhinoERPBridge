using System;
using System.Runtime.InteropServices;
using Eto.Forms;
using Eto.Drawing;
using System.Collections.Generic;
using System.Linq;
using RhinoERPBridge.Data;
using RhinoERPBridge.Models;
using Rhino;
using Rhino.Input.Custom;
using Rhino.DocObjects;

namespace RhinoERPBridge.UI
{
    [Guid("6A6A74A3-6F6C-4F2D-89C1-7A0A3F3C7E5F")]
    public class ErpSearchPanel : Panel
    {
        public static Guid PanelGuid => typeof(ErpSearchPanel).GUID;

        private readonly TextBox _searchBox;
        private readonly Button _searchButton;
        private readonly Label _statusLabel;
        private readonly Label _activeTableLabel;
        private readonly GridView _grid;
        private readonly CheckBox _showAllColumns;
        private readonly NumericStepper _rowLimit;
        private readonly Button _updateButton;
        private IArticleRepository _repo;
        private readonly Button _settingsButton;
        private readonly Button _dsnSettingsButton;
        private readonly Button _normalViewButton;
        private int? _lastContextColumnIndex;

        public ErpSearchPanel()
        {
            var layout = new DynamicLayout { Spacing = new Size(6, 6), Padding = new Padding(8) };

            layout.AddRow(new Label { Text = "ERP Article Search", Font = SystemFonts.Bold(SystemFonts.Default().Size + 1) });

            _searchBox = new TextBox { PlaceholderText = "Search term..." };
            _searchButton = new Button { Text = "Search" };
            _statusLabel = new Label { Text = "Ready", TextColor = Colors.Gray };
            _activeTableLabel = new Label { Text = string.Empty, TextColor = Colors.Gray };
            _settingsButton = new Button { Text = "DB Settings..." };
            _dsnSettingsButton = new Button { Text = "DSN Settings..." };
            _showAllColumns = new CheckBox { Text = "Show all columns" };
            _rowLimit = new NumericStepper { MinValue = 1, MaxValue = 10000, Increment = 50, Value = 100 };
            _updateButton = new Button { Text = "Update" };
            _normalViewButton = new Button { Text = "Normal view" };

            _grid = BuildGrid();

            _searchButton.Click += (s, e) => ApplySearch();
            _searchBox.KeyDown += (s, e) => { if (e.Key == Keys.Enter) ApplySearch(); };

            // Context menu for copy actions
            var miCopyValue = new ButtonMenuItem { Text = "Copy value" };
            miCopyValue.Click += (s, e) => CopySelectedValue();
            var miCopyRow = new ButtonMenuItem { Text = "Copy row" };
            miCopyRow.Click += (s, e) => CopySelectedRow();
            var miCopyValueWithHeader = new ButtonMenuItem { Text = "Copy value with header" };
            miCopyValueWithHeader.Click += (s, e) => CopySelectedValue(withHeader: true);
            var miCopyRowWithHeader = new ButtonMenuItem { Text = "Copy row with header" };
            miCopyRowWithHeader.Click += (s, e) => CopySelectedRow(withHeader: true);
            var miExportValue = new ButtonMenuItem { Text = "Export value to Rhino object" };
            miExportValue.Click += (s, e) => ExportSelectedValueToRhino();
            var miExportRow = new ButtonMenuItem { Text = "Export row to Rhino object" };
            miExportRow.Click += (s, e) => ExportSelectedRowToRhino();

            // Column chooser submenus for precise selection regardless of horizontal scroll
            var chooseValueMenu = new ButtonMenuItem { Text = "Copy value (choose column)" };
            var chooseExportValueMenu = new ButtonMenuItem { Text = "Export value (choose column)" };
            _grid.ContextMenu = new ContextMenu(
                miCopyValue,
                miCopyValueWithHeader,
                chooseValueMenu,
                miCopyRow,
                miCopyRowWithHeader,
                new SeparatorMenuItem(),
                miExportValue,
                miExportRow,
                chooseExportValueMenu
            );

            _grid.ContextMenu.Opening += (s, e) =>
            {
                // rebuild column chooser items
                chooseValueMenu.Items.Clear();
                chooseExportValueMenu.Items.Clear();
                for (int i = 0; i < _grid.Columns.Count; i++)
                {
                    var colIndex = i; // capture
                    var header = _grid.Columns[i].HeaderText ?? $"Col {i + 1}";
                    chooseValueMenu.Items.Add(new ButtonMenuItem { Text = header, Command = new Command((_, __) => CopyValueByColumn(colIndex, withHeader: false)) });
                    chooseExportValueMenu.Items.Add(new ButtonMenuItem { Text = header, Command = new Command((_, __) => ExportValueByColumnToRhino(colIndex)) });
                }
            };
            _grid.MouseDown += (s, e) =>
            {
                if (e.Buttons == MouseButtons.Alternate)
                {
                    _lastContextColumnIndex = EstimateColumnIndex(e.Location.X);
                }
            };

            // Search term on its own row (full width)
            layout.Add(_searchBox, xscale: true);
            // Action buttons in one row sharing the width (use nested TableLayout so main layout stays single-column)
            var buttonsRow = new TableLayout(
                new TableRow(
                    new TableCell(_searchButton, true),
                    new TableCell(_settingsButton, true),
                    new TableCell(_dsnSettingsButton, true),
                    new TableCell(_normalViewButton, true)
                )
            );
            layout.AddRow(buttonsRow);

            // Status + active table + row controls on one line
            var statusRow = new TableLayout(new TableRow(
                new TableCell(_statusLabel),
                new TableCell(_activeTableLabel, true),
                new TableCell(new Label { Text = "Rows:" }),
                new TableCell(_rowLimit),
                new TableCell(_updateButton)
            ));
            layout.AddRow(statusRow);
            layout.AddRow(_showAllColumns);
            layout.Add(_grid, xscale: true, yscale: true);

            Content = layout;

            // repo selection
            _repo = CreateRepository();
            UpdateActiveTableName();
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
            _dsnSettingsButton.Click += (s, e) => Rhino.UI.Panels.OpenPanel(DsnSettingsPanel.PanelGuid);
            _showAllColumns.CheckedChanged += (s, e) => ApplySearch();
            _normalViewButton.Click += (s, e) => { _showAllColumns.Checked = false; ApplySearch(); };
            _updateButton.Click += (s, e) => ApplySearch();
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

            RebuildTypedColumns(grid);

            return grid;
        }

        private void RebuildTypedColumns()
        {
            RebuildTypedColumns(_grid);
        }

        private void RebuildTypedColumns(GridView grid)
        {
            grid.Columns.Clear();
            grid.Columns.Add(new GridColumn { HeaderText = "SKU", DataCell = new TextBoxCell { Binding = Binding.Property<Article, string>(a => a.Sku) }, Width = 120 });
            grid.Columns.Add(new GridColumn { HeaderText = "Name", DataCell = new TextBoxCell { Binding = Binding.Property<Article, string>(a => a.Name) }, Width = 220 });
            grid.Columns.Add(new GridColumn { HeaderText = "Category", DataCell = new TextBoxCell { Binding = Binding.Property<Article, string>(a => a.Category) }, Width = 120 });
            grid.Columns.Add(new GridColumn { HeaderText = "Price", DataCell = new TextBoxCell { Binding = Binding.Property<Article, string>(a => a.Price.ToString("F2")) }, Width = 80 });
            grid.Columns.Add(new GridColumn { HeaderText = "Stock", DataCell = new TextBoxCell { Binding = Binding.Property<Article, string>(a => a.Stock.ToString()) }, Width = 70 });
        }

        private void ApplySearch()
        {
            var term = _searchBox.Text ?? string.Empty;
            // Always recreate repository to pick up latest DB Settings
            _repo = CreateRepository();
            UpdateActiveTableName();
            if (_showAllColumns.Checked == true)
            {
                TryBindDynamic(term);
                return;
            }
            // Ensure typed columns are restored after dynamic mode
            RebuildTypedColumns();
            var results = _repo.Search(term);
            _statusLabel.Text = $"{results.Count} results";
            BindArticles(results);
        }

        private void TryBindDynamic(string term)
        {
            try
            {
                UpdateActiveTableName();
                var settings = RhinoERPBridge.Services.SettingsService.Load();
                if (settings == null || !settings.IsConfigured || string.IsNullOrWhiteSpace(settings.ArticlesTable))
                {
                    _statusLabel.Text = "DB Settings not configured";
                    _statusLabel.TextColor = Colors.IndianRed;
                    return;
                }

                var data = RhinoERPBridge.Data.DynamicSqlHelper.QueryTop(settings, term, (int)_rowLimit.Value);
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

        private void UpdateActiveTableName()
        {
            var settings = RhinoERPBridge.Services.SettingsService.Load();
            var tbl = settings?.ArticlesTable;
            _activeTableLabel.Text = string.IsNullOrWhiteSpace(tbl) ? string.Empty : $"    /    Active table: {tbl}";
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

        private void CopySelectedValue(bool withHeader = false)
        {
            var text = GetSelectedValueText(withHeader);
            if (!string.IsNullOrEmpty(text))
            {
                var cb = new Clipboard();
                cb.Text = text;
            }
        }

        private void CopySelectedRow(bool withHeader = false)
        {
            var text = GetSelectedRowText(withHeader);
            if (!string.IsNullOrEmpty(text))
            {
                var cb = new Clipboard();
                cb.Text = text;
            }
        }

        private string GetSelectedValueText(bool withHeader)
        {
            var item = _grid.SelectedItem;
            if (item == null) return null;
            var colIndex = _lastContextColumnIndex ?? 0;
            if (item is Article a)
            {
                var (header, val) = GetArticleCell(colIndex, a);
                return withHeader ? $"{header}\t{val}" : val;
            }
            if (item is Dictionary<string, object> d)
            {
                var hdr = (_grid.Columns.Count > colIndex && colIndex >= 0) ? _grid.Columns[colIndex].HeaderText : null;
                if (!string.IsNullOrEmpty(hdr) && d.TryGetValue(hdr, out var v) && v != null)
                    return withHeader ? $"{hdr}\t{v}" : v.ToString();
                return null;
            }
            return item.ToString();
        }

        private string GetSelectedRowText(bool withHeader)
        {
            var item = _grid.SelectedItem;
            if (item == null) return null;
            if (item is Article a)
            {
                var cells = new (string header, string val)[]
                {
                    ("SKU", a.Sku),
                    ("Name", a.Name),
                    ("Category", a.Category),
                    ("Price", a.Price.ToString("F2")),
                    ("Stock", a.Stock.ToString())
                };
                return withHeader
                    ? string.Join("\t", cells.Select(c => c.header)) + "\n" + string.Join("\t", cells.Select(c => c.val))
                    : string.Join("\t", cells.Select(c => c.val));
            }
            if (item is Dictionary<string, object> d)
            {
                var headers = _grid.Columns.Select(c => c.HeaderText);
                var values = _grid.Columns.Select(c =>
                {
                    if (d.TryGetValue(c.HeaderText, out var v) && v != null) return v.ToString();
                    return string.Empty;
                });
                return withHeader
                    ? string.Join("\t", headers) + "\n" + string.Join("\t", values)
                    : string.Join("\t", values);
            }
            return item.ToString();
        }

        private (string header, string val) GetArticleCell(int colIndex, Article a)
        {
            switch (colIndex)
            {
                case 0: return ("SKU", a.Sku);
                case 1: return ("Name", a.Name);
                case 2: return ("Category", a.Category);
                case 3: return ("Price", a.Price.ToString("F2"));
                case 4: return ("Stock", a.Stock.ToString());
                default: return ("Name", a.Name);
            }
        }

        private int? EstimateColumnIndex(float mouseX)
        {
            if (_grid.Columns.Count == 0) return 0;
            float acc = 0;
            for (int i = 0; i < _grid.Columns.Count; i++)
            {
                var w = _grid.Columns[i].Width > 0 ? _grid.Columns[i].Width : 120;
                if (mouseX >= acc && mouseX < acc + w)
                    return i;
                acc += w;
            }
            return _grid.Columns.Count - 1;
        }

        private void CopyValueByColumn(int colIndex, bool withHeader)
        {
            _lastContextColumnIndex = colIndex;
            CopySelectedValue(withHeader);
        }

        private void ExportValueByColumnToRhino(int colIndex)
        {
            _lastContextColumnIndex = colIndex;
            ExportSelectedValueToRhino();
        }

        private void ExportSelectedValueToRhino()
        {
            var item = _grid.SelectedItem;
            if (item == null) return;
            var doc = RhinoDoc.ActiveDoc;
            if (doc == null) return;
            var go = new GetObject();
            go.SetCommandPrompt("Select object to attach user text");
            go.GeometryFilter = ObjectType.AnyObject;
            var res = go.Get();
            if (go.CommandResult() != Rhino.Commands.Result.Success) return;
            var obj = go.Object(0).Object();
            if (obj == null) return;

            var colIndex = _lastContextColumnIndex ?? 0;
            string key;
            string value;
            if (item is Article a)
            {
                var cell = GetArticleCell(colIndex, a);
                key = cell.header;
                value = cell.val;
            }
            else if (item is Dictionary<string, object> d)
            {
                key = (_grid.Columns.Count > colIndex && colIndex >= 0) ? _grid.Columns[colIndex].HeaderText : "Value";
                d.TryGetValue(key, out var v);
                value = v?.ToString();
            }
            else
            {
                key = "Value";
                value = item.ToString();
            }

            var attr = obj.Attributes.Duplicate();
            attr.SetUserString(key, value ?? string.Empty);
            doc.Objects.ModifyAttributes(obj.Id, attr, true);
            doc.Views.Redraw();
        }

        private void ExportSelectedRowToRhino()
        {
            var item = _grid.SelectedItem;
            if (item == null) return;
            var doc = RhinoDoc.ActiveDoc;
            if (doc == null) return;
            var go = new GetObject();
            go.SetCommandPrompt("Select object to attach user text (row)");
            go.GeometryFilter = ObjectType.AnyObject;
            var res = go.Get();
            if (go.CommandResult() != Rhino.Commands.Result.Success) return;
            var obj = go.Object(0).Object();
            if (obj == null) return;

            var attr = obj.Attributes.Duplicate();
            if (item is Article a)
            {
                attr.SetUserString("SKU", a.Sku);
                attr.SetUserString("Name", a.Name);
                attr.SetUserString("Category", a.Category);
                attr.SetUserString("Price", a.Price.ToString("F2"));
                attr.SetUserString("Stock", a.Stock.ToString());
            }
            else if (item is Dictionary<string, object> d)
            {
                foreach (var col in _grid.Columns)
                {
                    var header = col.HeaderText ?? "";
                    if (string.IsNullOrEmpty(header)) continue;
                    if (d.TryGetValue(header, out var v) && v != null)
                        attr.SetUserString(header, v.ToString());
                }
            }
            doc.Objects.ModifyAttributes(obj.Id, attr, true);
            doc.Views.Redraw();
        }
    }
}


