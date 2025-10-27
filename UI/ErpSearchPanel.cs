using System;
using System.Runtime.InteropServices;
using Eto.Forms;
using Eto.Drawing;
using System.Collections.Generic;
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
        private IArticleRepository _repo;
        private readonly Button _settingsButton;

        public ErpSearchPanel()
        {
            var layout = new DynamicLayout { Spacing = new Size(6, 6), Padding = new Padding(8) };

            layout.Add(new Label { Text = "ERP Article Search", Font = SystemFonts.Bold(SystemFonts.Default().Size + 1) });

            _searchBox = new TextBox { PlaceholderText = "Search term..." };
            _searchButton = new Button { Text = "Search" };
            _statusLabel = new Label { Text = "Ready", TextColor = Colors.Gray };
            _settingsButton = new Button { Text = "DB Settings..." };

            _grid = BuildGrid();

            _searchButton.Click += (s, e) => ApplySearch();
            _searchBox.KeyDown += (s, e) => { if (e.Key == Keys.Enter) ApplySearch(); };

            layout.BeginHorizontal();
            layout.Add(_searchBox, xscale: true);
            layout.Add(_searchButton);
            layout.Add(_settingsButton);
            layout.EndHorizontal();

            layout.AddRow(_statusLabel);
            layout.Add(_grid, yscale: true);

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
            var results = _repo.Search(term);
            _statusLabel.Text = $"{results.Count} results";
            BindArticles(results);
        }

        private void BindArticles(IReadOnlyList<Article> items)
        {
            _grid.DataStore = items;
        }
    }
}


