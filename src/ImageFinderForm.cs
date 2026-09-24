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
		private List<ImageComparisonResult> oneToAllResults = [];
		private List<EachToEachComparisonResult> eachToEachResults = [];
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
			if (ImageComparisonResult.EachToEachMode)
				EachToEachRadio.Checked = true;
			else
				OneToAllRadio.Checked = true;
			OneToAllRadio_CheckedChanged (null, null);

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
			ImageComparisonResult.EachToEachMode = EachToEachRadio.Checked;
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

			RDLocale.SetControlText (OneToAllRadio);
			RDLocale.SetControlText (EachToEachRadio);

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
			StartSearch.Enabled = (((sampleHash != null) || EachToEachRadio.Checked) &&
				Directory.Exists (DirectoryPath.Text));
			}

		private void DirectoryPath_TextChanged (object sender, EventArgs e)
			{
			CheckSearch ();
			}

		// Инициализация поиска
		private void StartSearch_Click (object sender, EventArgs e)
			{
			// Получение списка файлов
			/*LoadedPicture.Visible = ViewBox.Visible = false;*/
			imageFiles.Clear ();
			/*SaveSettings ();*/

			// Сбор списка файлов
			for (int i = 0; i < supportedImageFormats.Length; i++)
				{
				try
					{
					imageFiles.AddRange (Directory.GetFiles (DirectoryPath.Text, "*." + supportedImageFormats[i],
						IncludeSubdirsFlag.Checked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
					}
				catch { }
				}

			if (imageFiles.Count < (EachToEachRadio.Checked ? 2 : 1))
				{
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText | RDMessageFlags.LockSmallSize,
					"NoImagesError");
				return;
				}

			// Блокировка интерфейса
			SaveSettings ();
			LoadedPicture.Visible = ViewBox.Visible = false;

			// Запуск в режиме с образцом
			if (OneToAllRadio.Checked)
				{
				// Пропуск образца, если он оказался в той же директории
				int idx = imageFiles.IndexOf (OFDialog.FileName);
				if (idx >= 0)
					imageFiles.RemoveAt (idx);

				// Выполнение
				RDInterface.RunWork (OneToAllSearch, null, "...", RDRunWorkFlags.AllowOperationAbort |
					RDRunWorkFlags.CaptionInTheMiddle);
				if (RDInterface.WorkResultAsInteger != 0)
					{
					RDInterface.LocalizedMessageBox (RDMessageFlags.CenterText | RDMessageFlags.Warning,
						"SearchInterruptedMessage");
					}

				// Загрузка результатов
				oneToAllResults.Sort ();
				ResultsList.Items.Clear ();

				for (int i = 0; (i < oneToAllResults.Count) && (i < maxResults); i++)
					{
					string result = oneToAllResults[i].ComparisonResult;

					if (!oneToAllResults[i].IsInited)
						result += " " + RDLocale.GetText ("BadImageMessage");

					string name = oneToAllResults[i].ImageName;
					if (name.Length > 30)
						name = name.Substring (0, 29) + "…";
					name = name.PadRight (30);
					ResultsList.Items.Add (name + " ".PadLeft (3) + "[" +
						oneToAllResults[i].ImageTransformType + "]" + " ".PadLeft (8) + result);
					}
				}

			// Запуск в режиме «каждый с каждым»
			else
				{
				// Выполнение
				RDInterface.RunWork (EachToEachSearch, null, "...", RDRunWorkFlags.AllowOperationAbort |
					RDRunWorkFlags.CaptionInTheMiddle);
				if (RDInterface.WorkResultAsInteger != 0)
					{
					RDInterface.LocalizedMessageBox (RDMessageFlags.CenterText | RDMessageFlags.Warning,
						"SearchInterruptedMessage");
					}

				// Загрузка результатов
				eachToEachResults.Sort ();
				ResultsList.Items.Clear ();

				for (int i = 0; (i < eachToEachResults.Count) && (i < maxResults); i++)
					{
					string result = eachToEachResults[i].ComparisonResultString;

					if (eachToEachResults[i].ComparisonResult == 0.0)
						result += " " + RDLocale.GetText ("BadImageMessage");

					int idx = (int)eachToEachResults[i].FirstImageIndex;
					string name1 = oneToAllResults[idx].ImageName;
					if (name1.Length > 16)
						name1 = name1.Substring (0, 15) + "…";
					name1 = name1.PadRight (16);
					name1 += " [" + oneToAllResults[idx].ImageTransformType + "]";

					idx = (int)eachToEachResults[i].SecondImageIndex;
					string name2 = oneToAllResults[idx].ImageName;
					if (name2.Length > 16)
						name2 = name2.Substring (0, 15) + "…";
					name2 = name2.PadRight (16);
					name2 += " [" + oneToAllResults[idx].ImageTransformType + "]";

					ResultsList.Items.Add (name1 + " × " + name2 + ":".PadRight (3) + result);
					}
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

		private void OneToAllSearch (object sender, DoWorkEventArgs e)
			{
			// Инициализация
			BackgroundWorker bw = ((BackgroundWorker)sender);
			oneToAllResults.Clear ();

			// Выполнение
			ImageTransformTypes[] itts = ImageComparisonResult.GetTransformTypes ();

			for (int i = 0; i < imageFiles.Count; i++)
				{
				// Возврат прогресса
				bw.ReportProgress ((int)((i + 1) * RDWorkerForm.ProgressBarSize / imageFiles.Count),
					string.Format (RDLocale.GetText ("ImageProcessingMessage"), Path.GetFileName (imageFiles[i]),
					i + 1, imageFiles.Count));

				// Добавление
				for (int t = 0; t < itts.Length; t++)
					{
					oneToAllResults.Add (new ImageComparisonResult (imageFiles[i], itts[t]));
					int idx = oneToAllResults.Count - 1;
					if (!oneToAllResults[idx].IsInited)
						continue;

					if (oneToAllResults[idx].MakeComparison (sampleHash) == 0.0)
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

		private void EachToEachSearch (object sender, DoWorkEventArgs e)
			{
			// Инициализация
			BackgroundWorker bw = ((BackgroundWorker)sender);
			oneToAllResults.Clear ();
			eachToEachResults.Clear ();
			sampleHash = null;

			// Сбор списка компараторов
			ImageTransformTypes[] itts = ImageComparisonResult.GetTransformTypes ();

			for (int i = 0; i < imageFiles.Count; i++)
				{
				// Возврат прогресса
				bw.ReportProgress ((int)((i + 1) * RDWorkerForm.ProgressBarSize / imageFiles.Count),
					string.Format (RDLocale.GetText ("ImageProcessingMessage"),
					Path.GetFileName (imageFiles[i]), i + 1, imageFiles.Count));

				// Добавление
				for (int t = 0; t < itts.Length; t++)
					{
					oneToAllResults.Add (new ImageComparisonResult (imageFiles[i], itts[t]));

					// Сравнение на этом шаге не проводится

					// Завершение работы, если получено требование от диалога
					if (bw.CancellationPending)
						{
						e.Result = 1;
						e.Cancel = true;
						return;
						}
					}
				}

			// Сравнение
			int count = oneToAllResults.Count * (oneToAllResults.Count - 1) / 2;
			int step = count / (int)RDWorkerForm.ProgressBarSize;
			if (step < 1)
				step = 1;

			int item = 0;
			for (int i1 = 0; i1 < oneToAllResults.Count; i1++)
				{
				for (int i2 = i1 + 1; i2 < oneToAllResults.Count; i2++)
					{
					// Возврат прогресса
					item++;
					if ((item - 1) % step == 0)
						{
						bw.ReportProgress ((int)(item * RDWorkerForm.ProgressBarSize / count),
							string.Format (RDLocale.GetText ("PairProcessingMessage"),
							Path.GetFileName (oneToAllResults[i1].ImageName),
							Path.GetFileName (oneToAllResults[i2].ImageName),
							item, count));
						}

					// Завершение работы, если получено требование от диалога
					if (bw.CancellationPending)
						{
						e.Result = 1;
						e.Cancel = true;
						return;
						}

					// Сравнение
					EachToEachComparisonResult etecr;
					etecr.FirstImageIndex = (uint)i1;
					etecr.SecondImageIndex = (uint)i2;

					if (!oneToAllResults[i1].IsInited || !oneToAllResults[i2].IsInited)
						etecr.ComparisonResult = 0.0;
					else
						etecr.ComparisonResult = oneToAllResults[i1].MakeComparison (oneToAllResults[i2].ImageHash);

					// Отсечка заведомо лишних результатов
					eachToEachResults.Add (etecr);
					if ((item % maxResults == 0) && (item > maxResults))
						{
						eachToEachResults.Sort ();
						eachToEachResults.RemoveRange ((int)maxResults, eachToEachResults.Count - (int)maxResults);
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

			if (ImageComparisonResult.EachToEachMode && (LoadedPicture.BackgroundImage != null))
				{
				LoadedPicture.BackgroundImage.Dispose ();
				LoadedPicture.BackgroundImage = null;
				}

			// Загрузка
			int idx = ResultsList.SelectedIndex;
			if (idx < 0)
				return;

			if (!ImageComparisonResult.EachToEachMode)
				{
				if (!oneToAllResults[idx].IsInited)
					return;

				ViewBox.BackgroundImage = oneToAllResults[idx].GetImage ();

				FirstFileButton.Text = oneToAllResults[idx].FullPath;
				FirstFileButton.Enabled = File.Exists (oneToAllResults[idx].FullPath);
				SecondFileButton.Text = "";
				SecondFileButton.Enabled = false;
				}
			else
				{
				if (eachToEachResults[idx].ComparisonResult == 0.0)
					return;

				int fidx = (int)eachToEachResults[idx].FirstImageIndex;
				LoadedPicture.BackgroundImage = oneToAllResults[fidx].GetImage ();
				FirstFileButton.Text = oneToAllResults[fidx].FullPath;
				FirstFileButton.Enabled = File.Exists (oneToAllResults[fidx].FullPath);

				fidx = (int)eachToEachResults[idx].SecondImageIndex;
				ViewBox.BackgroundImage = oneToAllResults[fidx].GetImage ();
				SecondFileButton.Text = oneToAllResults[fidx].FullPath;
				SecondFileButton.Enabled = File.Exists (oneToAllResults[fidx].FullPath);
				}
			}

		// Переключение режима обработки
		private void OneToAllRadio_CheckedChanged (object sender, EventArgs e)
			{
			SelectImage.Enabled = OneToAllRadio.Checked;
			CheckSearch ();
			}

		// Удаление дубликатов
		private void FirstFileButton_Click (object sender, EventArgs e)
			{
			// Запрос варианта
			Button b = (Button)sender;
			RDMessageButtons res = RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText | RDMessageFlags.LockSmallSize,
				string.Format (RDLocale.GetText ("RemoveFileMessage"), Path.GetFileName (b.Text)),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Replace),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Delete));

			// Отмена
			if (res == RDMessageButtons.ButtonOne)
				return;

			// Удаление
			if (res == RDMessageButtons.ButtonOne)
				{
				try
					{
					File.Delete (b.Text);
					b.Enabled = false;
					}
				catch { }
				return;
				}

			// Изменение имени
			string path = Path.GetDirectoryName (b.Text);
			if (!path.EndsWith ('\\'))
				path += "\\";
			path += Path.GetFileNameWithoutExtension (b.Text);
			string ext = Path.GetExtension (b.Text);

			try
				{
				File.Move (path + ext, path + ".bak");
				b.Text = path + ".bak";
				}
			catch { }
			}
		}
	}
