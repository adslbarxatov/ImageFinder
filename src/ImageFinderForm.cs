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

			LocalizeForm (null, null);
			}

		// Выход из программы
		private void BExit_Click (object sender, EventArgs e)
			{
			this.Close ();
			}

		private void ImageFinderForm_FormClosing (object sender, FormClosingEventArgs e)
			{
			RDGenerics.SaveWindowDimensions (this);
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

			/*int w = b.Width * LoadedPicture.Height / b.Height;
			LoadedPicture.BackgroundImage = new Bitmap (b, w, LoadedPicture.Height);
			b.Dispose ();*/
			LoadedPicture.BackgroundImage = b2;

			CheckSearch ();
			/*byte[] data2 = File.ReadAllBytes (RDGenerics.StartupPath + "1.dat");

			double c = ImageMath.CompareHash (data, data2);
			c *= 100;*/
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
			for (int i = 0; i < supportedImageFormats.Length; i++)
				{
				try
					{
					imageFiles.AddRange (Directory.GetFiles (DirectoryPath.Text, "*." + supportedImageFormats[i],
						SearchOption.AllDirectories));
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
				/*string line = comparisonResults[i].ImageName + ": " +
					comparisonResults[i].ComparisonResult;*/
				string line = comparisonResults[i].ComparisonResult;

				if (!comparisonResults[i].IsInited)
					line += " " + RDLocale.GetText ("BadImageMessage");
				/*else if ((i > 0) && (line == comparisonResults[i - 1].ComparisonResult))
					line += " " + RDLocale.GetText ("PossibleMatchMessage");*/

				ResultsList.Items.Add (comparisonResults[i].ImageName + ": " + line);
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
			for (int i = 0; i < imageFiles.Count; i++)
				{
				bw.ReportProgress ((int)((i + 1) * RDWorkerForm.ProgressBarSize / imageFiles.Count),
					string.Format (RDLocale.GetText ("ImageProcessingMessage"), Path.GetFileName (imageFiles[i]),
					i + 1, imageFiles.Count));  // Возврат прогресса

				// Добавление
				comparisonResults.Add (new ImageComparisonResult (imageFiles[i]));
				if (!comparisonResults[i].IsInited)
					continue;

				if (!comparisonResults[i].MakeComparison (sampleHash))
					continue;

				// Завершение работы, если получено требование от диалога
				if (bw.CancellationPending)
					{
					e.Result = 1;
					e.Cancel = true;
					return;
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
