using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using EnterprisePOS.Domain;
using EnterprisePOS.Services;

namespace EnterprisePOS.WinForms
{
    /// <summary>One dialog for both "Add product" (productId = null) and "Edit product".</summary>
    internal sealed class FrmProductEditor : ThemedDialog
    {
        private readonly InventoryService inventory = new InventoryService();
        private readonly int? productId;

        private readonly TextBox txtSku = Theme.Input(30);
        private readonly TextBox txtBarcode = Theme.Input(50);
        private readonly TextBox txtName = Theme.Input(150);
        private readonly ComboBox cmbCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.Regular };
        private readonly NumericUpDown numPrice = Theme.Num(0m, 99999999m, 2);
        private readonly NumericUpDown numCost = Theme.Num(0m, 99999999m, 2);
        private readonly NumericUpDown numStock = Theme.Num(0m, 10000000m, 0);
        private readonly NumericUpDown numReorder = Theme.Num(0m, 10000000m, 0);
        private readonly CheckBox chkActive = new CheckBox { Text = "Product is active", Checked = true, AutoSize = true, Font = Theme.Regular };
        private readonly PictureBox picture = new PictureBox
        {
            Size = new Size(96, 96),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
            Location = new Point(0, 4)
        };

        private byte[] newImage;
        private bool imageChanged;

        public int SavedProductId { get; private set; }

        public FrmProductEditor(int? productId)
            : base(productId.HasValue ? "Edit product" : "Add product",
                   productId.HasValue ? "Update the product details" : "Create a new product", 520, 700)
        {
            this.productId = productId;

            AddRow("SKU *", txtSku);
            AddRow("Barcode", txtBarcode);
            AddRow("Product name *", txtName);
            AddRow("Category *", cmbCategory);
            AddRow("Selling price *", numPrice);
            AddRow("Cost price", numCost);
            AddRow(productId.HasValue ? "Stock (read-only)" : "Opening stock", numStock);
            AddRow("Reorder level", numReorder);

            if (productId.HasValue)
            {
                numStock.Enabled = false;
                AddRow("Status", chkActive);
            }

            Panel imagePanel = new Panel { Height = 108, MinimumSize = new Size(0, 108) };
            Button browse = Theme.Btn("Choose image...", ButtonKind.Neutral, 130);
            Button clear = Theme.Btn("Remove", ButtonKind.Neutral, 90);
            browse.Location = new Point(110, 4);
            clear.Location = new Point(250, 4);
            browse.Click += (s, e) => BrowseImage();
            clear.Click += (s, e) => ClearImage();
            Label hint = Theme.Lbl("JPG or PNG, up to 5 MB.", Theme.Small, Theme.Muted);
            hint.Location = new Point(110, 46);
            imagePanel.Controls.Add(picture);
            imagePanel.Controls.Add(browse);
            imagePanel.Controls.Add(clear);
            imagePanel.Controls.Add(hint);
            AddRow("Image", imagePanel);

            Button save = Theme.Btn("Save product", ButtonKind.Success, 130);
            Button cancel = Theme.Btn("Cancel", ButtonKind.Neutral, 90);
            cancel.DialogResult = DialogResult.Cancel;
            save.Click += (s, e) => Save();
            AddButton(save);
            AddButton(cancel);
            AcceptButton = save;
            CancelButton = cancel;

            Load += (s, e) => LoadData();
        }

        private void LoadData()
        {
            try
            {
                cmbCategory.DataSource = inventory.GetCategories();
                cmbCategory.DisplayMember = "Name";
                cmbCategory.ValueMember = "CategoryId";
                cmbCategory.SelectedIndex = productId.HasValue ? cmbCategory.SelectedIndex : -1;

                if (!productId.HasValue) return;

                Product p = inventory.GetById(productId.Value);
                if (p == null)
                {
                    Theme.Warn(this, "That product no longer exists.");
                    DialogResult = DialogResult.Cancel;
                    return;
                }

                txtSku.Text = p.Sku;
                txtBarcode.Text = p.Barcode ?? string.Empty;
                txtName.Text = p.Name;
                cmbCategory.SelectedValue = p.CategoryId;
                numPrice.Value = Math.Min(numPrice.Maximum, p.SellingPrice);
                numCost.Value = Math.Min(numCost.Maximum, p.CostPrice);
                numStock.Value = Math.Min(numStock.Maximum, p.StockQuantity);
                numReorder.Value = Math.Min(numReorder.Maximum, p.ReorderLevel);
                chkActive.Checked = p.IsActive;
                if (p.Image != null && p.Image.Length > 0) ShowPreview(p.Image);
            }
            catch (Exception ex)
            {
                AppLog.Write(ex);
                Theme.Error(this, "Unable to load the product.\n\n" + ex.Message);
                DialogResult = DialogResult.Cancel;
            }
        }

        private void Save()
        {
            try
            {
                if (cmbCategory.SelectedValue == null) throw new ArgumentException("Choose a category.");

                Product p = new Product
                {
                    Id = productId ?? 0,
                    Sku = txtSku.Text,
                    Barcode = txtBarcode.Text,
                    Name = txtName.Text,
                    CategoryId = Convert.ToInt32(cmbCategory.SelectedValue),
                    SellingPrice = numPrice.Value,
                    CostPrice = numCost.Value,
                    StockQuantity = (int)numStock.Value,
                    ReorderLevel = (int)numReorder.Value,
                    IsActive = chkActive.Checked
                };

                SavedProductId = inventory.Save(p, imageChanged ? newImage : null, imageChanged);
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                Theme.Warn(this, ex.Message);
            }
        }

        // ------------------------------------------------------------ image handling

        private void BrowseImage()
        {
            using (OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                CheckFileExists = true
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    if (new FileInfo(dialog.FileName).Length > 5L * 1024 * 1024)
                        throw new InvalidOperationException("Choose an image smaller than 5 MB.");

                    byte[] png;
                    using (Image source = Image.FromFile(dialog.FileName))
                    {
                        png = ToPng(source, 400);
                    }

                    newImage = png;
                    imageChanged = true;
                    ShowPreview(png);
                }
                catch (OutOfMemoryException)
                {
                    Theme.Warn(this, "That file is not a valid JPG or PNG image.");
                }
                catch (Exception ex)
                {
                    Theme.Warn(this, ex.Message);
                }
            }
        }

        private void ClearImage()
        {
            newImage = null;
            imageChanged = true;
            if (picture.Image != null) { picture.Image.Dispose(); picture.Image = null; }
        }

        private void ShowPreview(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            using (Image temp = Image.FromStream(ms))
            {
                Image old = picture.Image;
                picture.Image = new Bitmap(temp);
                if (old != null) old.Dispose();
            }
        }

        /// <summary>Re-encodes as PNG and limits the longest side, which also strips anything unexpected from the file.</summary>
        private static byte[] ToPng(Image source, int maxSide)
        {
            float scale = Math.Min(1f, (float)maxSide / Math.Max(source.Width, source.Height));
            int w = Math.Max(1, (int)(source.Width * scale));
            int h = Math.Max(1, (int)(source.Height * scale));

            using (Bitmap bmp = new Bitmap(w, h))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(source, 0, 0, w, h);
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }
    }
}
