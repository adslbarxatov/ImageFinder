using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Главная форма программы
	/// </summary>
	public partial class ImageFinderForm: Form
		{
		// Переменные
		private byte[] sampleHash;
		private List<ImageComparisonResult> comparisonResults = [];
		private List<string> imageFiles = [];

		private string[] supportedImageFormats = [
			"bmp", "png", "gif", "jpe", "jpeg", "jpg", "tif", "tiff"
			];

		private const uint maxResults = 100;

		/// <summary>
		/// Конструктор. Создаёт главную форму программы
		/// </summary>
		public ImageFinderForm ()
			{
			// Первичная настройка
			InitializeComponent ();

			// Настройка контролов
			this.Text = RDGenerics.DefaultAssemblyVisibleName;
			RDGenerics.LoadWindowDimensions (this);

			DirectoryPath.Text = ImageComparisonResult.ImagesDirectory;

			ImageTransformTypes itt = ImageComparisonResult.TransformTypes;
			CW0Flag.Checked = itt.HasFlag (ImageTransformTypes.CW0);
			CW90Flag.Checked = itt.HasFlag (ImageTransformTypes.CW90);
			CW180Flag.Checked = itt.HasFlag (ImageTransformTypes.CW180);
			CW270Flag.Checked = itt.HasFlag (ImageTransformTypes.CW270);
			NoFlipFlag.Checked = itt.HasFlag (ImageTransformTypes.NoFlip);
			HFlipFlag.Checked = itt.HasFlag (ImageTransformTypes.HFlip);
			VFlipFlag.Checked = itt.HasFlag (ImageTransformTypes.VFlip);
			BFlipFlag.Checked = itt.HasFlag (ImageTransformTypes.BFlip);

			IncludeSubdirsFlag.Checked = ImageComparisonResult.IncludeSubdirectories;

			LocalizeForm (null, null);
			}

		// Выход из программы
		private void BExit_Click (object sender, EventArgs e)
			{
			this.Close ();
			}

		private void ImageFinderForm_FormClosing (object sender, FormClosingEventArgs e)
			{
			SaveSettings ();
			RDGenerics.SaveWindowDimensions (this);
			}

		// Сохранение настроек обработки изображений
		private void SaveSettings ()
			{
			ImageComparisonResult.ImagesDirectory = DirectoryPath.Text;

			if (!CW0Flag.Checked && !CW90Flag.Checked && !CW180Flag.Checked && !CW270Flag.Checked)
				CW0Flag.Checked = true;
			if (!NoFlipFlag.Checked && !HFlipFlag.Checked && !VFlipFlag.Checked && !BFlipFlag.Checked)
				NoFlipFlag.Checked = true;

			ImageTransformTypes itt = 0x00;
			if (CW0Flag.Checked)
				itt |= ImageTransformTypes.CW0;
			if (CW90Flag.Checked)
				itt |= ImageTransformTypes.CW90;
			if (CW180Flag.Checked)
				itt |= ImageTransformTypes.CW180;
			if (CW270Flag.Checked)
				itt |= ImageTransformTypes.CW270;
			if (NoFlipFlag.Checked)
				itt |= ImageTransformTypes.NoFlip;
			if (HFlipFlag.Checked)
				itt |= ImageTransformTypes.HFlip;
			if (VFlipFlag.Checked)
				itt |= ImageTransformTypes.VFlip;
			if (BFlipFlag.Checked)
				itt |= ImageTransformTypes.BFlip;
			ImageComparisonResult.TransformTypes = itt;

			ImageComparisonResult.IncludeSubdirectories = IncludeSubdirsFlag.Checked;
			}

		// Справочные сведения
		private void MAbout_Click (object sender, EventArgs e)
			{
			RDInterface.ShowAbout (false);
			}

		// Локализация формы
		private void LocalizeForm (object sender, EventArgs e)
			{
			// Запрос языка
			if ((sender != null) && !RDInterface.MessageBox ())
				return;

			RDLocale.SetDefaultControlText (MExit, RDLDefaultTexts.Button_Exit);
			RDLocale.SetDefaultControlText (MAbout, RDLDefaultTexts.Control_AppAbout);
			RDLocale.SetDefaultControlText (MLanguage, RDLDefaultTexts.Control_InterfaceLanguage);
			
			RDLocale.SetControlText (MOptions);
			RDLocale.SetControlText (StartSearch);
			RDLocale.SetControlText (Label05);
			RDLocale.SetControlText (SelectImage);
			RDLocale.SetControlText (Label02);

			RDLocale.SetControlText (Label03);
			
			RDLocale.SetControlText (CW0Flag);
			CW0Flag.Text = "α: " + CW0Flag.Text;
			RDLocale.SetControlText (CW90Flag);
			CW90Flag.Text = "β: " + CW90Flag.Text;
			RDLocale.SetControlText (CW180Flag);
			CW180Flag.Text = "δ: " + CW180Flag.Text;
			RDLocale.SetControlText (CW270Flag);
			CW270Flag.Text = "θ: " + CW270Flag.Text;

			RDLocale.SetControlText (NoFlipFlag);
			NoFlipFlag.Text = "λ: " + NoFlipFlag.Text;
			RDLocale.SetControlText (HFlipFlag);
			HFlipFlag.Text = "ξ: " + HFlipFlag.Text;
			RDLocale.SetControlText (VFlipFlag);
			VFlipFlag.Text = "ψ: " + VFlipFlag.Text;
			RDLocale.SetControlText (BFlipFlag);
			BFlipFlag.Text = "ω: " + BFlipFlag.Text;

			RDLocale.SetControlText (IncludeSubdirsFlag);

			OFDialog.Filter = RDLocale.GetText ("ImageFilter");
			for (int i = 0; i < supportedImageFormats.Length; i++)
				OFDialog.Filter += ("*." + supportedImageFormats[i] +
					(i < supportedImageFormats.Length - 1 ? ";" : ""));
			}

		// Выбор изображения
		private void SelectImage_Click (object sender, EventArgs e)
			{
			OFDialog.ShowDialog ();
			}

		private void OFDialog_FileOk (object sender, CancelEventArgs e)
			{
			// Сброс картинки
			if (LoadedPicture.BackgroundImage != null)
				{
				LoadedPicture.BackgroundImage.Dispose ();
				LoadedPicture.BackgroundImage = null;
				}

			// Попытка загрузки
			Bitmap b1;
			try
				{
				b1 = new Bitmap (OFDialog.FileName);
				}
			catch
				{
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"ImageLoadingError");
				CheckSearch ();
				return;
				}

			// Отвязывание
			Bitmap b2 = new Bitmap (b1);
			b1.Dispose ();

			// Обработка
			sampleHash = ImageComparisonResult.GetImageHash (b2);
			LoadedPicture.BackgroundImage = b2;

			CheckSearch ();
			}

		// Выбор директории с изображениями
		private void SelectDirectoryPath_Click (object sender, EventArgs e)
			{
			if (!string.IsNullOrWhiteSpace (DirectoryPath.Text))
				FBDialog.SelectedPath = DirectoryPath.Text;

			if (FBDialog.ShowDialog () != DialogResult.OK)
				return;

			DirectoryPath.Text = FBDialog.SelectedPath;
			}

		// Разблокировка кнопки поиска
		private void CheckSearch ()
			{
			StartSearch.Enabled = ((LoadedPicture.BackgroundImage != null) &&
				Directory.Exists (DirectoryPath.Text));
			}

		private void DirectoryPath_TextChanged (object sender, EventArgs e)
			{
			CheckSearch ();
			}

		// Инициализация поиска
		private void StartSearch_Click (object sender, EventArgs e)
			{
			// Сбор списка файлов
			LoadedPicture.Visible = ViewBox.Visible = false;
			imageFiles.Clear ();
			SaveSettings ();

			for (int i = 0; i < supportedImageFormats.Length; i++)
				{
				try
					{
					imageFiles.AddRange (Directory.GetFiles (DirectoryPath.Text, "*." + supportedImageFormats[i],
						ImageComparisonResult.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
					}
				catch { }
				}

			if (imageFiles.Count < 1)
				{
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"NoImagesError");
				return;
				}

			// Пропуск образца, если он оказался в той же директории
			int idx = imageFiles.IndexOf (OFDialog.FileName);
			if (idx >= 0)
				imageFiles.RemoveAt (idx);

			// Запуск
			RDInterface.RunWork (Search, null, "...", RDRunWorkFlags.AllowOperationAbort |
				RDRunWorkFlags.CaptionInTheMiddle);
			if (RDInterface.WorkResultAsInteger != 0)
				{
				RDInterface.LocalizedMessageBox (RDMessageFlags.CenterText | RDMessageFlags.Warning,
					"SearchInterruptedMessage");
				}

			// Загрузка результатов
			comparisonResults.Sort ();
			ResultsList.Items.Clear ();

			for (int i = 0; (i < comparisonResults.Count) && (i < maxResults); i++)
				{
				string result = comparisonResults[i].ComparisonResult;

				if (!comparisonResults[i].IsInited)
					result += " " + RDLocale.GetText ("BadImageMessage");

				string name = comparisonResults[i].ImageName.PadRight (30) + " ".PadLeft (3) + "[" +
					comparisonResults[i].ImageTransformType + "]" + " ".PadLeft (8);
				ResultsList.Items.Add (name + result);
				}

			// Отображение
			ResultsList.Enabled = true;
			LoadedPicture.Visible = ViewBox.Visible = true;

			if (ResultsList.Items.Count > 0)
				{
				ResultsList.SelectedIndex = 0;
				ResultsList_SelectedIndexChanged (null, null);
				}
			}

		private void Search (object sender, DoWorkEventArgs e)
			{
			// Инициализация
			BackgroundWorker bw = ((BackgroundWorker)sender);
			comparisonResults.Clear ();

			// Выполнение
			ImageTransformTypes[] itts = ImageComparisonResult.GetTransformTypes ();

			for (int i = 0; i < imageFiles.Count; i++)
				{
				bw.ReportProgress ((int)((i + 1) * RDWorkerForm.ProgressBarSize / imageFiles.Count),
					string.Format (RDLocale.GetText ("ImageProcessingMessage"), Path.GetFileName (imageFiles[i]),
					i + 1, imageFiles.Count));	// Возврат прогресса

				// Добавление
				for (int t = 0; t < itts.Length; t++)
					{
					comparisonResults.Add (new ImageComparisonResult (imageFiles[i], itts[t]));
					int idx = comparisonResults.Count - 1;
					if (!comparisonResults[idx].IsInited)
						continue;

					if (!comparisonResults[idx].MakeComparison (sampleHash))
						continue;

					// Завершение работы, если получено требование от диалога
					if (bw.CancellationPending)
						{
						e.Result = 1;
						e.Cancel = true;
						return;
						}
					}
				}

			// Завершено
			e.Result = 0;
			}

		// Просмотр изображения
		private void ResultsList_SelectedIndexChanged (object sender, EventArgs e)
			{
			// Инициализация
			if (ViewBox.BackgroundImage != null)
				{
				ViewBox.BackgroundImage.Dispose ();
				ViewBox.BackgroundImage = null;
				}

			// Загрузка
			int idx = ResultsList.SelectedIndex;
			if (idx < 0)
				return;

			if (!comparisonResults[idx].IsInited)
				return;

			ViewBox.BackgroundImage = comparisonResults[idx].GetImage ();
			}
		}
	}
