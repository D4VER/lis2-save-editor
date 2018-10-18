﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Windows.Forms;
using lis2_save_editor.Properties;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace lis2_save_editor
{
    public partial class MainForm : Form
    {
        private readonly SettingManager _settingManager = new SettingManager();

        public MainForm()
        {
            InitializeComponent();
            ValidatePaths();
        }

        private GameSave _gameSave;
        private List<string> _steamIdFolders = new List<string>();

        private List<dynamic> _editedControls = new List<object>();

        private bool SaveLoading = false;

        private void buttonLoad_Click(object sender, EventArgs e)
        {
            _gameSave = new GameSave();
            _gameSave.ReadSaveFromFile(textBoxSavePath.Text);

            if (!_gameSave.SaveIsValid)
            {
                MessageBox.Show(Resources.CorruptSaveMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string[] cpName;
            if (_gameSave.saveType == SaveType.CaptainSpirit)
            {
                cpName = _gameSave.Data["CheckpointName"].Value.Split('_');
            }
            else
            {
                cpName = _gameSave.Data["CurrentSubContextSaveData"].Value["CheckpointName"].Value.Split('_');
            }
            SaveLoading = true;
            comboBoxSelectCP.Items.Clear();
            comboBoxSelectCP.Items.Add("Current - "+ _gameSave.Data["CurrentSubContextSaveData"].Value["SubContextId"].Value 
                                       +"_"+cpName[cpName.Length-1]);
            comboBoxHeader_EPName.Items.Clear();
            comboBoxHeader_SubContextName.Items.Clear();
            comboBoxCPName.Items.Clear();
            ClearGroupBox(groupBoxLISHeader);
            if (_gameSave.saveType == SaveType.LIS)
            {
                for (int i = 1; i <= _gameSave.Data["CheckpointHistory"].ElementCount; i++)
                {
                    comboBoxSelectCP.Items.Add(String.Format("{0} - {1}",
                    i, _gameSave.Data["CheckpointHistory"].Value[i]["SubContextId"].Value));
                }

                comboBoxCPName.Items.AddRange(GameInfo.LIS2_CheckpointNames);

                var header = _gameSave.Data["HeaderSaveData"].Value;
                comboBoxHeader_EPName.Items.AddRange(GameInfo.LIS2_EpisodeNames);
                comboBoxHeader_SubContextName.Items.AddRange(GameInfo.LIS2_SubContextNames.Values.ToArray());
                comboBoxHeader_EPName.SelectedIndex = Convert.ToInt32(header["EpisodeName"].Value[1].Substring(22, 1)) - 1;
                comboBoxHeader_EPNumber.SelectedIndex = header["EpisodeNumber"].Value - 1;
                if (header["SubContextName"].Value.Length > 0)
                {
                    comboBoxHeader_SubContextName.SelectedItem = GameInfo.LIS2_SubContextNames[header["SubContextName"].Value[1]];
                }
                else
                {
                    comboBoxHeader_SubContextName.SelectedItem = GameInfo.LIS2_SubContextNames["NONE"];
                }

                checkBoxGameStarted.Checked = header["bGameStarted"].Value;

                groupBoxLISHeader.Enabled = true;
                groupBoxDanielPos.Enabled = true;
                groupBoxAICall.Enabled = true;
                groupBoxEpisodeCompletion.Enabled = true;

            }
            else
            {
                comboBoxCPName.Items.AddRange(GameInfo.CS_CheckpointNames.ToArray());

                groupBoxLISHeader.Enabled = false;
                groupBoxDanielPos.Enabled = false;
                groupBoxAICall.Enabled = false;
                groupBoxEpisodeCompletion.Enabled = false;
            }
            comboBoxSelectCP.SelectedIndex = 0;

            tabControlMain.Enabled = true;
            comboBoxSelectCP.Enabled = true;
            buttonSaveEdits.Enabled = true;
            labelChangesWarning.Visible = false;
            _gameSave.SaveChangesSaved = true;
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            if (ModifierKeys == Keys.Control && File.Exists(textBoxSavePath.Text))
            {
                System.Diagnostics.Process.Start(Directory.GetParent(textBoxSavePath.Text).ToString());
            }
            else
            {
                DialogResult result = openFileDialog1.ShowDialog();
                if (result == DialogResult.OK)
                {
                    _settingManager.Settings.SavePath = openFileDialog1.FileName;
                    textBoxSavePath.Text = openFileDialog1.FileName;
                }
            }
        }

        private void buttonSaveEdits_Click(object sender, EventArgs e)
        {
            _gameSave.WriteSaveToFile(textBoxSavePath.Text);
            MessageBox.Show(Resources.EditsSuccessfullySavedMessage, "Savegame Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);

            ResetEditedControls();
            labelChangesWarning.Visible = false;
        }

        private void buttonAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show(String.Format(Resources.AboutMessage, Program.GetApplicationVersionStr()), 
                            "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #region Read functions

        private void UpdatePlayerInfo(int cpIndex)
        {
            ClearGroupBox(groupBoxPlayerPos);
            dynamic root;
            if (cpIndex == 0)
            {
                root = _gameSave.Data["CurrentSubContextSaveData"].Value["PlayerSaveData"].Value;
            }
            else
            {
                root = _gameSave.Data["CheckpointHistory"].Value[cpIndex]["PlayerSaveData"].Value;
            }

            dynamic pos = root["RespawnTransform"].Value;
            textBoxPlayerRotationX.Text = pos["Rotation"].Value["Quat"].X.ToString();
            textBoxPlayerRotationY.Text = pos["Rotation"].Value["Quat"].Y.ToString();
            textBoxPlayerRotationZ.Text = pos["Rotation"].Value["Quat"].Z.ToString();
            textBoxPlayerRotationW.Text = pos["Rotation"].Value["Quat"].W.ToString();
            textBoxPlayerTranslationX.Text = pos["Translation"].Value["Vector"].X.ToString();
            textBoxPlayerTranslationY.Text = pos["Translation"].Value["Vector"].Y.ToString();
            textBoxPlayerTranslationZ.Text = pos["Translation"].Value["Vector"].Z.ToString();
            textBoxPlayerScaleX.Text = pos["Scale3D"].Value["Vector"].X.ToString();
            textBoxPlayerScaleY.Text = pos["Scale3D"].Value["Vector"].Y.ToString();
            textBoxPlayerScaleZ.Text = pos["Scale3D"].Value["Vector"].Z.ToString();

            if (root["PlayerControllerDisplacementMode"].Value == "ELIS2DisplacementMode::CustomMode")
            {
                comboBoxPlayerDisplacementMode.SelectedItem = root["PlayerControllerCustomDisplacementModeId"].Value;
            }
            else
            {
                comboBoxPlayerDisplacementMode.SelectedItem = root["PlayerControllerDisplacementMode"].Value.Replace("ELIS2DisplacementMode::", "");
            }

            checkBoxLockedDiary.Checked = root["bPlayerControllerLockedDiary"].Value;
            checkBoxVoicePaused.Checked = root["bInnerVoiceComponentPaused"].Value;
        }

        private void UpdateDanielInfo(int cpIndex)
        {
            ClearGroupBox(groupBoxDanielPos);
            if (_gameSave.saveType == SaveType.CaptainSpirit)
            {
                return;
            }

            dynamic root;
            if (cpIndex == 0)
            {
                root = _gameSave.Data["CurrentSubContextSaveData"].Value["BrotherAISaveData"].Value;
            }
            else
            {
                root = _gameSave.Data["CheckpointHistory"].Value[cpIndex]["BrotherAISaveData"].Value;
            }

            dynamic pos = root["RespawnTransform"].Value;
            textBoxDanielRotationX.Text = pos["Rotation"].Value["Quat"].X.ToString();
            textBoxDanielRotationY.Text = pos["Rotation"].Value["Quat"].Y.ToString();
            textBoxDanielRotationZ.Text = pos["Rotation"].Value["Quat"].Z.ToString();
            textBoxDanielRotationW.Text = pos["Rotation"].Value["Quat"].W.ToString();
            textBoxDanielTranslationX.Text = pos["Translation"].Value["Vector"].X.ToString();
            textBoxDanielTranslationY.Text = pos["Translation"].Value["Vector"].Y.ToString();
            textBoxDanielTranslationZ.Text = pos["Translation"].Value["Vector"].Z.ToString();
            textBoxDanielScaleX.Text = pos["Scale3D"].Value["Vector"].X.ToString();
            textBoxDanielScaleY.Text = pos["Scale3D"].Value["Vector"].Y.ToString();
            textBoxDanielScaleZ.Text = pos["Scale3D"].Value["Vector"].Z.ToString();

            comboBoxDanielAIState.SelectedItem = root["AIState"].Value.Replace("ELIS2AIState::", "");
            comboBoxDanielPOI.SelectedItem = root["PointOfInterestInProgress"].Value; //todo - fill the combobox with items
            comboBoxDanielAIPreset.SelectedItem = root["AIDataPresetName"].Value;
        }

        private void UpdateAICallInfo(int cpIndex)
        {
            ClearGroupBox(groupBoxAICall);
            if (_gameSave.saveType == SaveType.CaptainSpirit)
            {
                return;
            }

            dynamic root;
            if (cpIndex == 0)
            {
                root = _gameSave.Data["CurrentSubContextSaveData"].Value["CallAISaveData"].Value;
            }
            else
            {
                root = _gameSave.Data["CheckpointHistory"].Value[cpIndex]["CallAISaveData"].Value;
            }

            checkBoxAICallGlobalEnable.Checked = root["bIsCallAIBehaviourEnable"].Value;
            checkBoxAICallFocusFail.Checked = root["bIsFocusFailNecessaryToCall"].Value;
            textBoxAICallDelay.Text = root["DelayBetweenCalls"].Value.ToString();

            List<dynamic> ai_items = root["CallAIItems"].Value;
            ai_items = ai_items.Skip(1).ToList();

            checkBoxAICall_Daniel.Checked = ai_items.Find(x => x["AIToCall"].Value.EndsWith("Daniel"))?["bEnable"].Value ?? false;
            checkBoxAICall_Cassidy.Checked = ai_items.Find(x => x["AIToCall"].Value.EndsWith("Cassidy"))?["bEnable"].Value ?? false;
            checkBoxAICall_Dog.Checked = ai_items.Find(x => x["AIToCall"].Value.EndsWith("Dog"))?["bEnable"].Value ?? false;
        }

        private void UpdateStats(int cpIndex)
        {
            ClearGroupBox(groupBoxEpisodeCompletion);
            if (_gameSave.saveType == SaveType.CaptainSpirit)
            {
                return;
            }
            foreach (var cb in groupBoxEpisodeCompletion.Controls.OfType<CheckBox>())
            {
                cb.Checked = false;
            }
            List<dynamic> root = _gameSave.Data["StatsSaveData"].Value["EpisodeCompletion"].Value;
            if (root.Count == 0)
            {
                return;
            }
            for (int i = 0; i < groupBoxEpisodeCompletion.Controls.Count; i++)
            {
                ((CheckBox)groupBoxEpisodeCompletion.Controls.Find("checkBoxEpComplete" + (i + 1), false)[0]).Checked = Convert.ToBoolean(root[i]);
            }
        }

        private void UpdateInventoryGrids(int cpIndex)
        {
            dataGridViewInventory1.Columns.Clear();
            dataGridViewInventory1.DataSource = BuildInventoryTable(cpIndex, "InventoryItems").DefaultView;

            dataGridViewInventory2.Columns.Clear();
            dataGridViewInventory2.DataSource = BuildInventoryTable(cpIndex, "BackPackItems").DefaultView;

            dataGridViewInventory3.Columns.Clear();
            dataGridViewInventory3.DataSource = BuildInventoryTable(cpIndex, "PocketsItems").DefaultView;
        }

        private DataTable BuildInventoryTable(int cpIndex, string inv_type)
        {
            DataTable t = new DataTable();
            List<dynamic> item_list;

            if(cpIndex == 0)
            {
                item_list = _gameSave.Data["CurrentSubContextSaveData"].Value["PlayerSaveData"]
                      .Value["PlayerInventorySaveData"].Value[inv_type].Value;
            }
            else
            {
                item_list = _gameSave.Data["CheckpointHistory"].Value[cpIndex]["PlayerSaveData"]
                      .Value["PlayerInventorySaveData"].Value[inv_type].Value;
            }

            object[] row = new object[t.Columns.Count];

            if (_gameSave.saveType == SaveType.CaptainSpirit)
            {
                t.Columns.Add("Name");
                t.Columns.Add("Quantity");

                row = new object[t.Columns.Count];

                foreach (var item in item_list.Skip(1))
                {
                    row[0] = item["PickupID"].Value;
                    row[1] = item["Quantity"].Value;
                    t.Rows.Add(row);
                }
            }
            else
            {
                t.Columns.Add("Name");
                t.Columns.Add("Quantity");
                t.Columns.Add("Is new", typeof(bool));

                row = new object[t.Columns.Count];

                foreach (var item in item_list.Skip(1))
                {
                    row[0] = item["PickupID"].Value;
                    row[1] = item["Quantity"].Value;
                    row[2] = item["bIsNew"].Value;
                    t.Rows.Add(row);
                }
            }
            
            return t;
        }

        private void UpdateSeenNotifsGrid(int cpIndex)
        {
            dataGridViewSeenNotifs.Columns.Clear();
            dataGridViewSeenNotifs.DataSource = BuildSeenNotifsTable(cpIndex).DefaultView;
        }

        private DataTable BuildSeenNotifsTable(int cpIndex)
        {
            DataTable t = new DataTable();
            List<dynamic> notif_list;
            if (cpIndex == 0)
            {
                notif_list = _gameSave.Data["CurrentSubContextSaveData"]
                             .Value["PlayerSaveData"].Value["AlreadySeenNotifications"].Value;
            }
            else
            {
                notif_list = _gameSave.Data["CheckpointHistory"].Value[cpIndex]
                             ["PlayerSaveData"].Value["AlreadySeenNotifications"].Value;
            }
            

            t.Columns.Add("Name");

            foreach (var element in notif_list)
            {
                t.Rows.Add(new object[] {element});
            }

            return t;
        }

        private void UpdateSeenTutosGrid(int cpIndex)
        {
            dataGridViewSeenTutos.Columns.Clear();
            dataGridViewSeenTutos.DataSource = BuildSeenTutosTable(cpIndex).DefaultView;
        }

        private DataTable BuildSeenTutosTable(int cpIndex)
        {
            DataTable t = new DataTable();
            dynamic tuto_list;

            if (cpIndex == 0)
            {
                tuto_list = _gameSave.Data["CurrentSubContextSaveData"]
                             .Value["PlayerSaveData"].Value["AlreadySeenTutorials"].Value;
            }
            else
            {
                tuto_list = _gameSave.Data["CheckpointHistory"].Value[cpIndex]
                            ["PlayerSaveData"].Value["AlreadySeenTutorials"].Value;
            }

            t.Columns.Add("Name");
            t.Columns.Add("Times");

            foreach (var element in tuto_list)
            {
                t.Rows.Add(new object[] { element.Key, element.Value });
            }
            return t;
        }

        private void UpdateDrawingsGrid(int cpIndex)
        {
            dataGridViewDrawings.Columns.Clear();

            List<dynamic> drawings;

            if (_gameSave.saveType == SaveType.CaptainSpirit)
            {
                return;
            }
            if (cpIndex == 0)
            {
                drawings = _gameSave.Data["CurrentSubContextSaveData"].Value["PlayerSaveData"]
                    .Value["DrawSequenceSaveData"].Value["DrawSequenceItemSaveDatas"].Value;
            }
            else
            {
                drawings = _gameSave.Data["CheckpointHistory"].Value[cpIndex]["PlayerSaveData"]
                    .Value["DrawSequenceSaveData"].Value["DrawSequenceItemSaveDatas"].Value;
            }

            DataGridViewComboBoxColumn combocol1 = new DataGridViewComboBoxColumn();
            combocol1.Items.AddRange("(none)", "Rough", "Detail", "Finished");
            combocol1.Name = "Part 1 Status";
            combocol1.HeaderText = "Part 1 Status";

            DataGridViewComboBoxColumn combocol2 = new DataGridViewComboBoxColumn();
            combocol2.Items.AddRange("(none)", "Rough", "Detail", "Finished");
            combocol2.Name = "Part 2 Status";
            combocol2.HeaderText = "Part 2 Status";

            dataGridViewDrawings.Columns.Add("Name", "Name");
            dataGridViewDrawings.Columns.Add(new DataGridViewCheckBoxColumn() {Name="Active", HeaderText="Active"});
            dataGridViewDrawings.Columns.Add("Part 1 Percent", "Part 1 Percent");
            dataGridViewDrawings.Columns.Add(combocol1);
            dataGridViewDrawings.Columns.Add("Part 2 Percent", "Part 2 Percent");
            dataGridViewDrawings.Columns.Add(combocol2);

            foreach (var item in GameInfo.LIS2_DrawingNames)
            {
                object[] row = new object[dataGridViewDrawings.Columns.Count];
                row[0] = item.Value;
                int index = drawings.FindIndex(1, x => x["DrawSequenceID"].Value["NameGuid"].Value["Guid"] == item.Key);
                if (index != -1)
                {
                    row[1] = true;
                    List<dynamic> data = drawings[index]["LandscapeItemSaveDatas"].Value;
                    data = data.Skip(1).ToList();

                    var ph1 = data.Find(x => x["LandscapeID"].Value == "Zone1_Reveal");
                    if (ph1 != null)
                    {
                        row[2] = ph1["DrawingPercent"].Value;
                        row[3] = ph1["DrawingPhase"].Value.Split(new string[] { "::" }, StringSplitOptions.None)[1];
                    }
                    else
                    {
                        row[2] = "";
                        row[3] = "(none)";
                    }

                    var ph2 = data.Find(x => x["LandscapeID"].Value == "Zone2_Reveal");
                    if (ph2 != null)
                    {
                        row[4] = ph2["DrawingPercent"].Value;
                        row[5] = ph2["DrawingPhase"].Value.Split(new string[] { "::" }, StringSplitOptions.None)[1];
                    }
                    else
                    {
                        row[4] = "";
                        row[5] = "(none)";
                    }
                }
                else
                {
                    row[1] = false;
                    row[2] = "";
                    row[3] = "(none)";
                    row[4] = "";
                    row[5] = "(none)";
                }
                dataGridViewDrawings.Rows.Add(row);
            }
        }

        private void UpdateAllFactsGrid(int cpIndex)
        {
            dataGridViewFacts.Columns.Clear();
            dataGridViewFacts.DataSource = BuildAllFactsTable(cpIndex).DefaultView;

            DataGridViewButtonColumn butcol = new DataGridViewButtonColumn();
            butcol.Name = "Value";
            butcol.Text = "View/Edit";
            butcol.UseColumnTextForButtonValue = true;
            dataGridViewFacts.Columns.Insert(1, butcol);

            dataGridViewFacts.Columns[0].ReadOnly = true;
            dataGridViewFacts.Columns[1].ReadOnly = true;
            dataGridViewFacts.Columns[1].Width = 30;
            dataGridViewFacts.Columns[2].Width = 100;
        }

        private DataTable BuildAllFactsTable(int cpIndex)
        {
            DataTable t = new DataTable();
            dynamic asset_list;

            if (cpIndex == 0)
            {
                asset_list = _gameSave.Data["CurrentSubContextSaveData"]
                             .Value["FactsSaveData"].Value;
            }
            else
            {
                asset_list = _gameSave.Data["CheckpointHistory"].Value[cpIndex]["FactsSaveData"].Value;
            }
            

            t.Columns.Add("Asset ID");
            t.Columns.Add("Keep values on save reset?");
            t.Columns[1].DataType = typeof(bool);
            object[] row = new object[t.Columns.Count];
            foreach (var asset in asset_list)
            {
                row[0] = asset.Key;
                row[1] = asset.Value["bKeepFactValuesOnSaveReset"].Value;
                t.Rows.Add(row);
            }
            return t;
        }

        private void UpdateWorldGrid(int cpIndex)
        {
            dataGridViewWorld.Columns.Clear();
            dataGridViewWorld.DataSource = BuildWorldTable(cpIndex).DefaultView;

            dataGridViewWorld.Columns[0].Width = 220;
        }

        private DataTable BuildWorldTable(int cpIndex)
        {
            DataTable t = new DataTable();
            List<dynamic> packages;
            if (cpIndex == 0)
            {
                packages = _gameSave.Data["CurrentSubContextSaveData"]
                             .Value["WorldStreamingSaveData"].Value;
            }
            else
            {
                packages = _gameSave.Data["CheckpointHistory"].Value[cpIndex]["WorldStreamingSaveData"].Value;
            }
            
            if (_gameSave.saveType == SaveType.CaptainSpirit)
            {
                t.Columns.Add("Package name");
                t.Columns.Add("Should be loaded");
                t.Columns.Add("Should be visible");
                t.Columns.Add("Should block on load");
                t.Columns.Add("Has loaded level");
                t.Columns.Add("Is visible");
                t.Columns[1].DataType = typeof(bool);
                t.Columns[2].DataType = typeof(bool);
                t.Columns[3].DataType = typeof(bool);
                t.Columns[4].DataType = typeof(bool);
                t.Columns[5].DataType = typeof(bool);
                object[] row = new object[t.Columns.Count];
                foreach (var pack in packages.Skip(1))
                {
                    row[0] = pack["StreamingLevelPackageName"].Value;
                    row[1] = pack["bShouldBeLoaded"].Value;
                    row[2] = pack["bShouldBeVisible"].Value;
                    row[3] = pack["bShouldBlockOnLoad"].Value;
                    row[4] = pack["bHasLoadedLevel"].Value;
                    row[5] = pack["bIsVisible"].Value;
                    t.Rows.Add(row);
                }
            }
            else
            {
                t.Columns.Add("Package name");
                t.Columns.Add("Should be loaded");
                t.Columns.Add("Should be visible");
                t.Columns.Add("Has loaded level");
                t.Columns.Add("Is visible");
                t.Columns.Add("Is requesting unload");
                t.Columns[1].DataType = typeof(bool);
                t.Columns[2].DataType = typeof(bool);
                t.Columns[3].DataType = typeof(bool);
                t.Columns[4].DataType = typeof(bool);
                t.Columns[5].DataType = typeof(bool);
                object[] row = new object[t.Columns.Count];
                foreach (var pack in packages.Skip(1))
                {
                    row[0] = pack["StreamingLevelPackageName"].Value;
                    row[1] = pack["bShouldBeLoaded"].Value;
                    row[2] = pack["bShouldBeVisible"].Value;
                    row[3] = pack["bHasLoadedLevel"].Value;
                    row[4] = pack["bIsVisible"].Value;
                    row[5] = pack["bIsRequestingUnloadAndRemoval"].Value;
                    t.Rows.Add(row);
                }
            }
            
            return t;
        }

        private void GenerateMetrics(int cpIndex)
        {
            tabPageMetrics.Controls.Clear();

            dynamic root;
            if (cpIndex == 0)
            {
                root = _gameSave.Data["CurrentSubContextSaveData"].Value["MetricsSaveData"].Value["MetricsBySection"].Value;
            }
            else
            {
                root = _gameSave.Data["CheckpointHistory"].Value[cpIndex]["MetricsSaveData"].Value["MetricsBySection"].Value;
            }
            
            int gbox_coord = 3, lbl_coord = 20, max_lbl_width = 0;
            foreach (var section in root)
            {
                var gbox = new GroupBox();
                gbox.AutoSize = true;
                gbox.Location = new Point(gbox_coord, 3);
                gbox.Text = section.Key;

                foreach (var cnt in section.Value["Counters"].Value)
                {
                    var lbl = new Label();
                    lbl.AutoSize = true;
                    lbl.Location = new Point(3, lbl_coord);
                    lbl.Text = cnt.Key.Split(':')[2];
                    gbox.Controls.Add(lbl);

                    if (lbl.Width > max_lbl_width)
                    {
                        max_lbl_width = lbl.Width;
                    }

                    var tb = new TextBox();
                    tb.Location = new Point(lbl.Location.X + 3, lbl.Location.Y);
                    tb.Name = "tb" + cnt.Key;
                    tb.Tag = section.Key + "::" + cnt.Key;
                    tb.Size = new Size(60, 20);
                    tb.Text = cnt.Value.ToString();
                    tb.TextChanged += new EventHandler(textBoxMetricsCounters_TextChanged);
                    gbox.Controls.Add(tb);
                    lbl_coord += 26;
                }
                foreach(var cnt in section.Value["TimeCounters"].Value)
                {
                    var lbl = new Label();
                    lbl.AutoSize = true;
                    lbl.Location = new Point(3, lbl_coord);
                    lbl.Text = cnt.Key.Split(':')[2] + " time";
                    gbox.Controls.Add(lbl);

                    if (lbl.Width > max_lbl_width)
                    {
                        max_lbl_width = lbl.Width;
                    }

                    var tb = new TextBox();
                    tb.Location = new Point(lbl.Location.X + 3, lbl.Location.Y);
                    tb.Name = "tb" + cnt.Key;
                    tb.Tag = section.Key + "::" + cnt.Key;
                    tb.Size = new Size(60, 20);
                    tb.Text = cnt.Value.ToString();
                    tb.TextChanged += new EventHandler(textBoxMetricsTime_TextChanged);
                    gbox.Controls.Add(tb);
                    lbl_coord += 26;
                }
                foreach (var cnt in section.Value["InteractionCounters"].Value)
                {
                    var lbl = new Label();
                    lbl.AutoSize = true;
                    lbl.Location = new Point(3, lbl_coord);
                    lbl.Text = cnt.Key + " interactions (total/unique)";
                    gbox.Controls.Add(lbl);

                    if (lbl.Width > max_lbl_width)
                    {
                        max_lbl_width = lbl.Width;
                    }

                    var tb1 = new TextBox();
                    tb1.Location = new Point(lbl.Location.X + 3, lbl.Location.Y);
                    tb1.Name = "tb" + cnt.Key + "_Total";
                    tb1.Tag = section.Key + "::Total::" + cnt.Key;
                    tb1.Size = new Size(60, 20);
                    tb1.Text = cnt.Value["Total"].Value.ToString();
                    tb1.TextChanged += new EventHandler(textBoxMetricsInteraction_TextChanged);
                    gbox.Controls.Add(tb1);

                    var tb2 = new TextBox();
                    tb2.Location = new Point(tb1.Location.X + tb1.Width + 10, lbl.Location.Y);
                    tb2.Name = "tb" + cnt.Key + "_Unique";
                    tb2.Tag = section.Key + "::Unique::" + cnt.Key;
                    tb2.Size = new Size(60, 20);
                    tb2.Text = cnt.Value["Unique"].Value.ToString();
                    tb2.TextChanged += new EventHandler(textBoxMetricsInteraction_TextChanged);
                    gbox.Controls.Add(tb2);
                    lbl_coord += 26;
                }

                lbl_coord = 20;

                foreach (var tb in gbox.Controls.OfType<TextBox>())
                {
                    tb.Location = new Point(tb.Location.X + max_lbl_width, tb.Location.Y);
                }

                tabPageMetrics.Controls.Add(gbox);
                
                gbox_coord += gbox.Width + 6;
            }
        }

        private void UpdateSeenPicturesGrid(int cpIndex)
        {
            dataGridViewSeenPics.Columns.Clear();
            dataGridViewSeenPics.DataSource = BuildSeenPicturesTable(cpIndex).DefaultView;

            if (_gameSave.saveType == SaveType.LIS)
            {
                dataGridViewSeenPics.Columns[1].Width = 40;
                dataGridViewSeenPics.Columns[2].Width = 40;
                dataGridViewSeenPics.Columns[3].Width = 60;
            }
        }

        private DataTable BuildSeenPicturesTable(int cpIndex)
        {
            DataTable t = new DataTable();
            t.Columns.Add("Name");
            t.Columns.Add("Seen", typeof(bool));
            List<dynamic> names;
            if (cpIndex == 0)
            {
                names = _gameSave.Data["CurrentSubContextSaveData"]
                             .Value["ShowPicturesSaveData"].Value["AllShowPictureIDSeen"].Value;
            }
            else
            {
                names = _gameSave.Data["CheckpointHistory"].Value[cpIndex]
                    ["ShowPicturesSaveData"].Value["AllShowPictureIDSeen"].Value;
            }

            if (_gameSave.saveType == SaveType.CaptainSpirit)
            {
                foreach (var item in GameInfo.CS_SeenPicturesNames)
                {
                    object[] row = new object[t.Columns.Count];
                    row[0] = item.Value;
                    int index = names.FindIndex(1, x => x["NameGuid"].Value["Guid"] == item.Key);
                    row[1] = (index != -1);
                    t.Rows.Add(row);
                }
            }
            else
            {

                t.Columns.Add("Obtained in CollectibleMode", typeof(bool));
                t.Columns.Add("Is new for SPMenu", typeof(bool));
                object[] row = new object[t.Columns.Count];

                foreach (var item in GameInfo.LIS2_SeenPicturesNames)
                {
                    row[0] = item.Value;
                    int index = names.FindIndex(1, x => x["ShowPictureID"].Value["NameGuid"].Value["Guid"] == item.Key);
                    if (index != -1)
                    {
                        row[1] = true;
                        row[2] = names[index]["bWasCollectedDuringCollectibleMode"].Value;
                        row[3] = names[index]["bIsNewForSPMenu"].Value;
                    }
                    else
                    {
                        row[1] = false;
                        row[2] = false;
                        row[3] = false;
                    }
                    t.Rows.Add(row);
                }
            }
            
            return t;
        }

        private void UpdateCollectiblesGrid(int cpIndex)
        {
            dataGridViewCollectibles.Columns.Clear();
            dataGridViewCollectibles.DataSource = BuildCollectiblesTable(cpIndex).DefaultView;
        }

        private DataTable BuildCollectiblesTable(int cpIndex)
        {
            DataTable t = new DataTable();
            if (_gameSave.saveType == SaveType.CaptainSpirit)
            {
                return t;
            }
            t.Columns.Add("Name");
            t.Columns.Add("Slot index");
            t.Columns.Add("Obtained in CollectibleMode", typeof(bool));
            t.Columns.Add("Is new", typeof(bool));

            List<dynamic> collectibles;
            if (cpIndex == 0)
            {
                collectibles = _gameSave.Data["CurrentSubContextSaveData"]
                             .Value["CollectiblesSaveData"].Value["Items"].Value;
            }
            else
            {
                collectibles = _gameSave.Data["CheckpointHistory"].Value[cpIndex]
                    ["CollectiblesSaveData"].Value["Items"].Value;
            }

            collectibles = collectibles.Skip(1).ToList();

            foreach (var item in GameInfo.LIS2_CollectibleNames)
            {
                object[] row = new object[t.Columns.Count];
                row[0] = item.Value;
                var coll = collectibles.Find(x => x["CollectibleGUID"].Value["Guid"] == item.Key);
                if (coll != null)
                {
                    row[1] = coll["EquipedSlotIndex"].Value;
                    row[2] = coll["bWasCollectedDuringCollectibleMode"].Value;
                    row[3] = coll["bIsNew"].Value;
                }
                else
                {
                    row[1] = "";
                    row[2] = false;
                    row[3] = false;
                }
                t.Rows.Add(row);
            }

            return t;
        }

        private void UpdateObjectivesGrid(int cpIndex)
        {
            dataGridViewObjectives.Columns.Clear();
            if (_gameSave.saveType == SaveType.CaptainSpirit)
            {
                return;
            }

            dataGridViewObjectives.Columns.Add("Name", "Name");

            DataGridViewComboBoxColumn combocol = new DataGridViewComboBoxColumn();
            combocol.Items.AddRange("(none)", "Active", "Done", "Aborted", "Inactive");
            combocol.Name = "State";
            combocol.HeaderText = "State";

            dataGridViewObjectives.Columns.Add(combocol);

            List<dynamic> objectives;
            if (cpIndex == 0)
            {
                objectives = _gameSave.Data["CurrentSubContextSaveData"]
                             .Value["ObjectiveSaveData"].Value["ObjectiveSaveDataItems"].Value;
            }
            else
            {
                objectives = _gameSave.Data["CheckpointHistory"].Value[cpIndex]
                    ["ObjectiveSaveData"].Value["ObjectiveSaveDataItems"].Value;
            }

            objectives = objectives.Skip(1).ToList();

            foreach (var item in GameInfo.LIS2_ObjectiveNames)
            {
                object[] row = new object[dataGridViewObjectives.Columns.Count];
                row[0] = item.Value;
                var obj = objectives.Find(x => x["ObjectiveGUID"].Value["Guid"] == item.Key);
                if (obj != null)
                {
                    row[1] = obj["ObjectiveState"].Value.Replace("ELIS2ObjectiveState::", "");
                }
                else
                {
                    row[1] = "(none)";
                }
                dataGridViewObjectives.Rows.Add(row);
            }

            dataGridViewObjectives.Columns["State"].Width = 120;
        }

        private void UpdateSeenMessagesGrid(int cpIndex)
        {
            dataGridViewSeenMessages.Columns.Clear();
            dataGridViewSeenMessages.DataSource = BuildSeenMessagesTable(cpIndex).DefaultView;
        }

        private DataTable BuildSeenMessagesTable(int cpIndex)
        {
            DataTable t = new DataTable();
            if (_gameSave.saveType == SaveType.CaptainSpirit)
            {
                return t;
            }
            t.Columns.Add("Name");
            t.Columns.Add("Seen", typeof(bool));

            List<dynamic> messages;
            if (cpIndex == 0)
            {
                messages = _gameSave.Data["CurrentSubContextSaveData"]
                             .Value["PhoneSaveData"].Value["ReadedMessages"].Value;
            }
            else
            {
                messages = _gameSave.Data["CheckpointHistory"].Value[cpIndex]
                    ["PhoneSaveData"].Value["ReadedMessages"].Value;
            }

            foreach (var item in GameInfo.LIS2_SMSNames)
            {
                object[] row = new object[t.Columns.Count];
                row[0] = item.Value;
                var msg = messages.Find(x => x == item.Key);
                if (msg != null)
                {
                    row[1] = true;
                }
                else
                {
                    row[1] = false;
                }
                t.Rows.Add(row);
            }

            return t;
        }

        #endregion

        private void dataGridViewFacts_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 1)
            {
                var editForm = new FactEditForm();
                var cpIndex = comboBoxSelectCP.SelectedIndex;
                string assetId = dataGridViewFacts[0, e.RowIndex].Value.ToString();
                if (cpIndex == 0)
                {
                    editForm.asset = _gameSave.Data["CurrentSubContextSaveData"].Value["FactsSaveData"].Value[assetId];
                }
                else
                {
                    editForm.asset = _gameSave.Data["CheckpointHistory"].Value[cpIndex]["FactsSaveData"].Value[assetId];
                }
                editForm.saveType = _gameSave.saveType;
                editForm.ShowDialog();
                if (editForm.changesMade)
                {
                    _editedControls.AddUnique(dataGridViewFacts[0, e.RowIndex]);
                    dataGridViewFacts[0, e.RowIndex].Style.BackColor = Color.LightGoldenrodYellow;
                    ShowChangesWarning();
                }
            }
        }

        #region Edit functions
        private void comboBoxCPName_SelectedValueChanged(object sender, EventArgs e)
        {
            if (!SaveLoading)
            {
                int cpIndex = comboBoxSelectCP.SelectedIndex;
                if (_gameSave.saveType == SaveType.CaptainSpirit)
                {
                    _gameSave.Data["CheckpointName"].Value = comboBoxCPName.SelectedItem.ToString();
                }
                else if (cpIndex == 0)
                {
                    _gameSave.Data["CurrentSubContextSaveData"].Value["CheckpointName"].Value = comboBoxCPName.SelectedItem.ToString();
                }
                else
                {
                    _gameSave.Data["CheckpointHistory"].Value[cpIndex]["CheckpointName"].Value = comboBoxCPName.SelectedItem.ToString();
                }

                _editedControls.AddUnique(panelCPName);
                panelCPName.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            } 
        }

        private void comboBoxEPName_SelectedValueChanged(object sender, EventArgs e)
        {
            if (!SaveLoading)
            {
                _gameSave.Data["HeaderSaveData"].Value["EpisodeName"].Value[1] = _gameSave.Data["HeaderSaveData"]
                .Value["EpisodeName"].Value[1].Substring(0, 22) + (comboBoxHeader_EPName.SelectedIndex + 1).ToString();

                _editedControls.AddUnique(panelEpName);
                panelEpName.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void comboBoxEPNumber_SelectedValueChanged(object sender, EventArgs e)
        {
            if (!SaveLoading)
            {
                _gameSave.Data["HeaderSaveData"].Value["EpisodeNumber"].Value = comboBoxHeader_EPNumber.SelectedIndex + 1;

                _editedControls.AddUnique(panelEpNumber);
                panelEpNumber.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void comboBoxSubContextName_SelectedValueChanged(object sender, EventArgs e)
        {
            if (!SaveLoading)
            {
                if (comboBoxHeader_SubContextName.SelectedIndex == 0)
                {
                    _gameSave.Data["HeaderSaveData"].Value["SubContextName"].Value = new string[0];
                }
                else
                {
                    _gameSave.Data["HeaderSaveData"].Value["SubContextName"].Value = new string[2]
                    {
                    "/Game/Localization/LocalizationAssets/E1/E1_Subcontexts.E1_Subcontexts",
                    GameInfo.LIS2_SubContextNames.ElementAt(comboBoxHeader_SubContextName.SelectedIndex).Key
                    };
                }

                _editedControls.AddUnique(panelSubContextName);
                panelSubContextName.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void checkBoxGameStarted_CheckedChanged(object sender, EventArgs e)
        {
            if (!SaveLoading)
            {
                _gameSave.Data["HeaderSaveData"].Value["bGameStarted"].Value = checkBoxGameStarted.Checked;

                _editedControls.AddUnique(checkBoxGameStarted);
                checkBoxGameStarted.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void textBoxMapName_TextChanged(object sender, EventArgs e)
        {
            if (!SaveLoading)
            {
                _gameSave.Data["MapName"].Value = textBoxMapName.Text;

                _editedControls.AddUnique(textBoxMapName);
                textBoxMapName.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void textBoxSubContextID_TextChanged(object sender, EventArgs e)
        {    
            if (!SaveLoading)
            {
                int cpIndex = comboBoxSelectCP.SelectedIndex;
                if (cpIndex == 0)
                {
                    _gameSave.Data["CurrentSubContextSaveData"].Value["SubContextId"].Value = textBoxSubContextID.Text;
                }
                else
                {
                    _gameSave.Data["CheckpointHistory"].Value[cpIndex]["SubContextId"].Value = textBoxSubContextID.Text;
                }

                _editedControls.AddUnique(textBoxSubContextID);
                textBoxSubContextID.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void textBoxSubContextPath_TextChanged(object sender, EventArgs e)
        {
            if (!SaveLoading)
            {
                _gameSave.Data["CurrentSubContextPathName"].Value = textBoxSubContextPath.Text;

                _editedControls.AddUnique(textBoxSubContextPath);
                textBoxSubContextPath.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void dateTimePickerSaveTime_ValueChanged(object sender, EventArgs e)
        {
            if (!SaveLoading)
            {
                _gameSave.Data["SaveTime"].Value["DateTime"] = dateTimePickerSaveTime.Value;

                _editedControls.AddUnique(panelSaveTime);
                panelSaveTime.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void textBoxPosition_TextChanged(object sender, EventArgs e)
        {
            if (!SaveLoading)
            {
                var tb = (TextBox)sender;
                //e.g Player::Rotation::X
                string[] info = tb.Tag.ToString().Split(new string[] { "::" }, 3, StringSplitOptions.RemoveEmptyEntries);
                float value = 0;
                try
                {
                    value = Convert.ToSingle(tb.Text);
                    tb.BackColor = Color.LightGoldenrodYellow;
                    _editedControls.AddUnique(tb);
                }
                catch
                {
                    tb.BackColor = Color.Red;
                }

                string dataType = (info[1] == "Rotation") ? "Quat" : "Vector";
                int cpIndex = comboBoxSelectCP.SelectedIndex;
                if (cpIndex == 0)
                {
                    switch (info[2])
                    {
                        case "X":
                            {
                                _gameSave.Data["CurrentSubContextSaveData"].Value[info[0] + "SaveData"]
                                .Value["RespawnTransform"].Value[info[1]].Value[dataType].X = value;
                                break;
                            }
                        case "Y":
                            {
                                _gameSave.Data["CurrentSubContextSaveData"].Value[info[0] + "SaveData"]
                                .Value["RespawnTransform"].Value[info[1]].Value[dataType].Y = value;
                                break;
                            }
                        case "Z":
                            {
                                _gameSave.Data["CurrentSubContextSaveData"].Value[info[0] + "SaveData"]
                                .Value["RespawnTransform"].Value[info[1]].Value[dataType].Z = value;
                                break;
                            }
                        case "W":
                            {
                                _gameSave.Data["CurrentSubContextSaveData"].Value[info[0] + "SaveData"]
                                .Value["RespawnTransform"].Value[info[1]].Value[dataType].W = value;
                                break;
                            }
                    }
                }
                else
                {
                    switch (info[2])
                    {
                        case "X":
                            {
                                _gameSave.Data["CheckpointHistory"].Value[cpIndex][info[0] + "SaveData"]
                                .Value["RespawnTransform"].Value[info[1]].Value[dataType].X = value;
                                break;
                            }
                        case "Y":
                            {
                                _gameSave.Data["CheckpointHistory"].Value[cpIndex][info[0] + "SaveData"]
                                .Value["RespawnTransform"].Value[info[1]].Value[dataType].Y = value;
                                break;
                            }
                        case "Z":
                            {
                                _gameSave.Data["CheckpointHistory"].Value[cpIndex][info[0] + "SaveData"]
                                .Value["RespawnTransform"].Value[info[1]].Value[dataType].Z = value;
                                break;
                            }
                        case "W":
                            {
                                _gameSave.Data["CheckpointHistory"].Value[cpIndex][info[0] + "SaveData"]
                                .Value["RespawnTransform"].Value[info[1]].Value[dataType].W = value;
                                break;
                            }
                    }
                }

                ShowChangesWarning();
            }
        }

        private void comboBoxPlayerDisplacementMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!SaveLoading)
            {
                dynamic root;
                var value = comboBoxPlayerDisplacementMode.SelectedItem.ToString();
                if (comboBoxSelectCP.SelectedIndex == 0)
                {
                    root = _gameSave.Data["CurrentSubContextSaveData"].Value["PlayerSaveData"].Value;
                }
                else
                {
                    root = _gameSave.Data["CheckpointHistory"].Value[comboBoxSelectCP.SelectedIndex]["PlayerSaveData"].Value;
                }

                if (comboBoxPlayerDisplacementMode.SelectedIndex > 2)
                {
                    root["PlayerControllerCustomDisplacementModeId"].Value = value;
                    root["PlayerControllerDisplacementMode"].Value = "ELIS2DisplacementMode::CustomMode";
                }
                else
                {
                    root["PlayerControllerCustomDisplacementModeId"].Value = "None";
                    root["PlayerControllerDisplacementMode"].Value = "ELIS2DisplacementMode::" + value;
                }

                _editedControls.AddUnique(panelPlayerDisplacementMode);
                panelPlayerDisplacementMode.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void checkBoxLockedDiary_CheckedChanged(object sender, EventArgs e)
        {
            if (!SaveLoading)
            {
                bool value = checkBoxLockedDiary.Checked;
                if (comboBoxSelectCP.SelectedIndex == 0)
                {
                    _gameSave.Data["CurrentSubContextSaveData"].Value["PlayerSaveData"].Value["bPlayerControllerLockedDiary"].Value = value;
                }
                else
                {
                    _gameSave.Data["CheckpointHistory"].Value[comboBoxSelectCP.SelectedIndex]["PlayerSaveData"].Value["bPlayerControllerLockedDiary"].Value = value;
                }

                _editedControls.AddUnique(checkBoxLockedDiary);
                checkBoxLockedDiary.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }

        }

        private void checkBoxVoicePaused_CheckedChanged(object sender, EventArgs e)
        {
            if (!SaveLoading)
            {
                bool value = checkBoxVoicePaused.Checked;
                if (comboBoxSelectCP.SelectedIndex == 0)
                {
                    _gameSave.Data["CurrentSubContextSaveData"].Value["PlayerSaveData"].Value["bInnerVoiceComponentPaused"].Value = value;
                }
                else
                {
                    _gameSave.Data["CheckpointHistory"].Value[comboBoxSelectCP.SelectedIndex]["PlayerSaveData"].Value["bInnerVoiceComponentPaused"].Value = value;
                }

                _editedControls.AddUnique(checkBoxVoicePaused);
                checkBoxVoicePaused.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void comboBoxDanielAIState_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!SaveLoading)
            {
                var value = comboBoxDanielAIState.SelectedItem.ToString();
                if (comboBoxSelectCP.SelectedIndex == 0)
                {
                    _gameSave.Data["CurrentSubContextSaveData"].Value["BrotherAISaveData"]
                             .Value["AIState"].Value = "ELIS2AIState::" + value;
                }
                else
                {
                    _gameSave.Data["CheckpointHistory"].Value[comboBoxSelectCP.SelectedIndex]["BrotherAISaveData"]
                             .Value["AIState"].Value = "ELIS2AIState::" + value;
                }

                _editedControls.AddUnique(panelDanielAIState);
                panelDanielAIState.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void comboBoxDanielPOI_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!SaveLoading)
            {
                var value = comboBoxDanielPOI.SelectedItem.ToString();
                if (comboBoxSelectCP.SelectedIndex == 0)
                {
                    _gameSave.Data["CurrentSubContextSaveData"].Value["BrotherAISaveData"]
                             .Value["PointOfInterestInProgress"].Value = value; //todo - check if this is correct
                }
                else
                {
                    _gameSave.Data["CheckpointHistory"].Value[comboBoxSelectCP.SelectedIndex]["BrotherAISaveData"]
                             .Value["PointOfInterestInProgress"].Value = value;
                }

                _editedControls.AddUnique(panelDanielPOI);
                panelDanielPOI.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void comboBoxDanielAIPreset_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!SaveLoading)
            {
                var value = comboBoxDanielAIPreset.SelectedItem.ToString();
                if (comboBoxSelectCP.SelectedIndex == 0)
                {
                    _gameSave.Data["CurrentSubContextSaveData"].Value["BrotherAISaveData"]
                             .Value["AIDataPresetName"].Value = value;
                }
                else
                {
                    _gameSave.Data["CheckpointHistory"].Value[comboBoxSelectCP.SelectedIndex]["BrotherAISaveData"]
                             .Value["AIDataPresetName"].Value = value;
                }

                _editedControls.AddUnique(panelDanielAIPreset);
                panelDanielAIPreset.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void textBoxAICallDelay_TextChanged(object sender, EventArgs e)
        {
            if (!SaveLoading)
            {
                float value;
                if (!float.TryParse(textBoxAICallDelay.Text, out value))
                {
                    value = 0;
                    textBoxAICallDelay.BackColor = Color.Red;
                }
                else
                {
                    textBoxAICallDelay.BackColor = Color.LightGoldenrodYellow;
                    _editedControls.AddUnique(textBoxAICallDelay);
                }

                if (comboBoxSelectCP.SelectedIndex == 0)
                {
                    _gameSave.Data["CurrentSubContextSaveData"].Value["CallAISaveData"].Value["DelayBetweenCalls"].Value = value;
                }
                else
                {
                    _gameSave.Data["CheckpointHistory"].Value[comboBoxSelectCP.SelectedIndex]["CallAISaveData"].Value["DelayBetweenCalls"].Value = value;
                }
            }
        }

        private void checkBoxAICallGlobalEnable_CheckedChanged(object sender, EventArgs e)
        {
            if (!SaveLoading)
            {
                bool value = checkBoxAICallGlobalEnable.Checked;
                if (comboBoxSelectCP.SelectedIndex == 0)
                {
                    _gameSave.Data["CurrentSubContextSaveData"].Value["CallAISaveData"].Value["bIsCallAIBehaviourEnable"].Value = value;
                }
                else
                {
                    _gameSave.Data["CheckpointHistory"].Value[comboBoxSelectCP.SelectedIndex]["CallAISaveData"].Value["bIsCallAIBehaviourEnable"].Value = value;
                }

                checkBoxAICallGlobalEnable.BackColor = Color.LightGoldenrodYellow;
                _editedControls.AddUnique(checkBoxAICallGlobalEnable);
            }
        }

        private void checkBoxAICallFocusFail_CheckedChanged(object sender, EventArgs e)
        {
            if (!SaveLoading)
            {
                bool value = checkBoxAICallFocusFail.Checked;
                if (comboBoxSelectCP.SelectedIndex == 0)
                {
                    _gameSave.Data["CurrentSubContextSaveData"].Value["CallAISaveData"].Value["bIsFocusFailNecessaryToCall"].Value = value;
                }
                else
                {
                    _gameSave.Data["CheckpointHistory"].Value[comboBoxSelectCP.SelectedIndex]["CallAISaveData"].Value["bIsFocusFailNecessaryToCall"].Value = value;
                }

                checkBoxAICallFocusFail.BackColor = Color.LightGoldenrodYellow;
                _editedControls.AddUnique(checkBoxAICallFocusFail);
            }
        }

        private void checkBoxAICall_CheckedChanged(object sender, EventArgs e)
        {
            if (!SaveLoading)
            {
                CheckBox cb = (CheckBox)sender;
                List<dynamic> root;
                if (comboBoxSelectCP.SelectedIndex == 0)
                {
                    root = _gameSave.Data["CurrentSubContextSaveData"].Value["CallAISaveData"].Value["CallAIItems"].Value;
                }
                else
                {
                    root = _gameSave.Data["CheckpointHistory"].Value[comboBoxSelectCP.SelectedIndex]["CallAISaveData"].Value["CallAIItems"].Value;
                }
                int index = root.FindIndex(1, x => x["AIToCall"].Value == "ELIS2AIBuddy::" + cb.Text);

                if (index == -1) //Add new
                {
                    Dictionary<string, dynamic> new_item = new Dictionary<string, dynamic>()
                    {
                        { "AIToCall", new EnumProperty()
                            {
                                Name = "AIToCall",
                                Type = "EnumProperty",
                                ElementType = "ELIS2AIBuddy",
                                Value = "ELIS2AIBuddy::"+cb.Text
                            }
                        },
                        { "bEnable", new BoolProperty()
                            {
                                Name = "bEnable",
                                Type="BoolProperty",
                                Value = cb.Checked
                            }
                        }
                    };
                }
                else //edit existing
                {
                    root[index]["bEnable"].Value = cb.Checked;
                }

                cb.BackColor = Color.LightGoldenrodYellow;
                _editedControls.AddUnique(cb);
            }

        }

        private void checkBoxEpComplete_CheckedChanged(object sender, EventArgs e)
        {
            if (!SaveLoading)
            {
                CheckBox cb = (CheckBox)sender;
                int index = Convert.ToInt32(cb.Text.Substring(cb.Text.Length - 1)) - 1;
                _gameSave.Data["StatsSaveData"].Value["EpisodeCompletion"].Value[index] = cb.Checked;

                cb.BackColor = Color.LightGoldenrodYellow;
                _editedControls.AddUnique(cb);
            }
        }

        private int newRowIndex = -1;

        private void dataGridView_UserAddedRow(object sender, DataGridViewRowEventArgs e)
        {
            newRowIndex = e.Row.Index-1;
        }

        private void dataGridView_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex == newRowIndex)
            {
                switch (((DataGridView)sender).Name)
                {
                    case "dataGridViewInventory1":
                        {
                            var name = dataGridViewInventory1[0, e.RowIndex].Value.ToString();
                            var qty = dataGridViewInventory1[1, e.RowIndex].Value;
                            if (qty is DBNull) qty = 0;
                            _gameSave.EditInventoryItem("InventoryItems", name, comboBoxSelectCP.SelectedIndex, 1, Convert.ToInt32(qty));
                            if (!(dataGridViewInventory1[2, e.RowIndex].Value is DBNull)) //if user checked "new" before clicking away from the row
                            {
                                _gameSave.EditInventoryItem("InventoryItems", name, comboBoxSelectCP.SelectedIndex, 2, Convert.ToInt32(true));
                            }
                            dataGridViewInventory1[1, e.RowIndex].Value = qty;
                            break;
                        }
                    case "dataGridViewInventory2":
                        {
                            var name = dataGridViewInventory2[0, e.RowIndex].Value.ToString();
                            var qty = dataGridViewInventory2[1, e.RowIndex].Value;
                            if (qty is DBNull) qty = 0;
                            _gameSave.EditInventoryItem("BackPackItems", name, comboBoxSelectCP.SelectedIndex, 1, Convert.ToInt32(qty));
                            if (!(dataGridViewInventory2[2, e.RowIndex].Value is DBNull)) //if user checked "new" before clicking away from the row
                            {
                                _gameSave.EditInventoryItem("BackPackItems", name, comboBoxSelectCP.SelectedIndex, 2, Convert.ToInt32(true));
                            }
                            dataGridViewInventory2[1, e.RowIndex].Value = qty;
                            break;
                        }
                    case "dataGridViewInventory3":
                        {
                            var name = dataGridViewInventory3[0, e.RowIndex].Value.ToString();
                            var qty = dataGridViewInventory3[1, e.RowIndex].Value;
                            if (qty is DBNull) qty = 0;
                            _gameSave.EditInventoryItem("PocketsItems", name, comboBoxSelectCP.SelectedIndex, 1, Convert.ToInt32(qty));
                            if (!(dataGridViewInventory3[2, e.RowIndex].Value is DBNull)) //if user checked "new" before clicking away from the row
                            {
                                _gameSave.EditInventoryItem("PocketsItems", name, comboBoxSelectCP.SelectedIndex, 2, Convert.ToInt32(true));
                            }
                            dataGridViewInventory3[1, e.RowIndex].Value = qty;
                            break;
                        }
                    case "dataGridViewSeenTutos":
                        {
                            var times = dataGridViewSeenTutos[1, e.RowIndex].Value;
                            if (times is DBNull) times = 0;
                            _gameSave.EditSeenTutorial(dataGridViewSeenTutos[0, e.RowIndex].Value.ToString(), comboBoxSelectCP.SelectedIndex, Convert.ToInt32(times));
                            dataGridViewSeenTutos[1, e.RowIndex].Value = times;
                            break;
                        }
                    case "dataGridViewSeenNotifs":
                        {
                            _gameSave.EditSeenNotification(dataGridViewSeenNotifs[0, e.RowIndex].Value.ToString(), comboBoxSelectCP.SelectedIndex, false);
                            break;
                        }
                    default:
                        {
                            throw new Exception("Unknown data grid");
                            break;
                        }
                }

                _editedControls.AddRange(((DataGridView)sender).Rows[e.RowIndex].Cells.Cast<DataGridViewCell>());
                foreach (DataGridViewCell cell in ((DataGridView)sender).Rows[e.RowIndex].Cells)
                {
                    cell.Style.BackColor = Color.LightGoldenrodYellow;
                }
                ShowChangesWarning();
            }
        }

        private void dataGridView_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            newRowIndex = -1;
        }

        private void dataGridView_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            var name = e.Row.Cells[0].Value.ToString();
            switch (((DataGridView)sender).Name)
            {
                case "dataGridViewInventory1":
                    {
                        _gameSave.EditInventoryItem("InventoryItems", name, comboBoxSelectCP.SelectedIndex, 0, null);
                        break;
                    }
                case "dataGridViewInventory2":
                    {
                        _gameSave.EditInventoryItem("BackPackItems", name, comboBoxSelectCP.SelectedIndex, 0, null);
                        break;
                    }
                case "dataGridViewInventory3":
                    {
                        _gameSave.EditInventoryItem("PocketsItems", name, comboBoxSelectCP.SelectedIndex, 0, null);
                        break;
                    }
                case "dataGridViewSeenTutos":
                    {
                        _gameSave.EditSeenTutorial(name, comboBoxSelectCP.SelectedIndex, null);
                        break;
                    }
                case "dataGridViewSeenNotifs":
                    {
                        _gameSave.EditSeenNotification(name, comboBoxSelectCP.SelectedIndex, true);
                        break;
                    }
                default:
                    {
                        throw new Exception("Unknown data grid");
                        break;
                    }
            }

            _editedControls.RemoveAll(x => (x is DataGridViewCell && x.RowIndex == e.Row.Index));
            ShowChangesWarning();
        }

        private void dataGridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            var grid = ((DataGridView)sender);
            if (grid.Rows[e.RowIndex].IsNewRow ^ e.ColumnIndex == 0)
            {
                e.Cancel = true;
                return;
            }
            origCellValue = grid[e.ColumnIndex, e.RowIndex].Value;
        }

        object origCellValue, newCellValue;

        private void dataGridViewInventory1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridViewInventory1.Rows[e.RowIndex].IsNewRow) return;

            switch (e.ColumnIndex)
            {
                case 0: return;
                case 1:
                    {
                        int result;
                        if (!int.TryParse(dataGridViewInventory1[1, e.RowIndex].Value.ToString(), out result))
                        {
                            MessageBox.Show(Resources.BadValueMessage, "Error");
                            newCellValue = origCellValue;
                            dataGridViewInventory1[e.ColumnIndex, e.RowIndex].Value = origCellValue;
                        }
                        else
                        {
                            newCellValue = result;
                        }
                        break;
                    }
                case 2:
                    {
                        newCellValue = dataGridViewInventory1[2, e.RowIndex].Value;
                        break;
                    }
            }
            

            if (newCellValue.ToString() != origCellValue.ToString())
            {
                var item_name = dataGridViewInventory1[0, e.RowIndex].Value.ToString(); 
                _gameSave.EditInventoryItem("InventoryItems", item_name, comboBoxSelectCP.SelectedIndex, e.ColumnIndex, Convert.ToInt32(newCellValue));
                _editedControls.AddUnique(dataGridViewInventory1[e.ColumnIndex, e.RowIndex]);
                dataGridViewInventory1[e.ColumnIndex, e.RowIndex].Style.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void dataGridViewInventory2_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridViewInventory2.Rows[e.RowIndex].IsNewRow) return;

            switch (e.ColumnIndex)
            {
                case 0: return;
                case 1:
                    {
                        int result;
                        if (!int.TryParse(dataGridViewInventory2[1, e.RowIndex].Value.ToString(), out result))
                        {
                            MessageBox.Show(Resources.BadValueMessage, "Error");
                            newCellValue = origCellValue;
                            dataGridViewInventory2[e.ColumnIndex, e.RowIndex].Value = origCellValue;
                        }
                        else
                        {
                            newCellValue = result;
                        }
                        break;
                    }
                case 2:
                    {
                        newCellValue = dataGridViewInventory2[2, e.RowIndex].Value;
                        break;
                    }
            }


            if (newCellValue.ToString() != origCellValue.ToString())
            {
                var item_name = dataGridViewInventory2[0, e.RowIndex].Value.ToString();
                _gameSave.EditInventoryItem("BackPackItems", item_name, comboBoxSelectCP.SelectedIndex, e.ColumnIndex, Convert.ToInt32(newCellValue));
                _editedControls.AddUnique(dataGridViewInventory2[e.ColumnIndex, e.RowIndex]);
                dataGridViewInventory2[e.ColumnIndex, e.RowIndex].Style.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void dataGridViewInventory3_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridViewInventory3.Rows[e.RowIndex].IsNewRow) return;

            switch (e.ColumnIndex)
            {
                case 0: return;
                case 1:
                    {
                        int result;
                        if (!int.TryParse(dataGridViewInventory3[1, e.RowIndex].Value.ToString(), out result))
                        {
                            MessageBox.Show(Resources.BadValueMessage, "Error");
                            newCellValue = origCellValue;
                            dataGridViewInventory3[e.ColumnIndex, e.RowIndex].Value = origCellValue;
                        }
                        else
                        {
                            newCellValue = result;
                        }
                        break;
                    }
                case 2:
                    {
                        newCellValue = dataGridViewInventory3[2, e.RowIndex].Value;
                        break;
                    }
            }


            if (newCellValue.ToString() != origCellValue.ToString())
            {
                var item_name = dataGridViewInventory3[0, e.RowIndex].Value.ToString();
                _gameSave.EditInventoryItem("PocketsItems", item_name, comboBoxSelectCP.SelectedIndex, e.ColumnIndex, Convert.ToInt32(newCellValue));
                _editedControls.AddUnique(dataGridViewInventory3[e.ColumnIndex, e.RowIndex]);
                dataGridViewInventory3[e.ColumnIndex, e.RowIndex].Style.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void dataGridViewSeenTutos_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0 || dataGridViewSeenTutos.Rows[e.RowIndex].IsNewRow) return;
            int result;
            if (!int.TryParse(dataGridViewSeenTutos[e.ColumnIndex, e.RowIndex].Value.ToString(), out result))
            {
                MessageBox.Show(Resources.BadValueMessage, "Error");
                newCellValue = origCellValue;
                dataGridViewSeenTutos[e.ColumnIndex, e.RowIndex].Value = origCellValue;
            }
            else
            {
                newCellValue = result;
            }

            if (newCellValue.ToString() != origCellValue.ToString())
            {
                var notif_name = dataGridViewSeenTutos[0, e.RowIndex].Value.ToString();
                _gameSave.EditSeenTutorial(notif_name, comboBoxSelectCP.SelectedIndex, Convert.ToInt32(newCellValue));
                _editedControls.AddUnique(dataGridViewSeenTutos[e.ColumnIndex, e.RowIndex]);
                dataGridViewSeenTutos[e.ColumnIndex, e.RowIndex].Style.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void dataGridViewFacts_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var name = dataGridViewFacts[0, e.RowIndex].Value.ToString();
            newCellValue = dataGridViewFacts[e.ColumnIndex, e.RowIndex].Value;
            if (newCellValue.ToString() != origCellValue.ToString())
            {
                int cpIndex = comboBoxSelectCP.SelectedIndex;
                if (cpIndex == 0)
                {
                    _gameSave.Data["CurrentSubContextSaveData"].Value["FactsSaveData"]
                             .Value[name]["bKeepFactValuesOnSaveReset"].Value = Convert.ToBoolean(newCellValue);
                }
                else
                {
                    _gameSave.Data["CheckpointHistory"].Value[cpIndex]["FactsSaveData"]
                             .Value[name]["bKeepFactValuesOnSaveReset"].Value = Convert.ToBoolean(newCellValue);
                }
                
                _editedControls.AddUnique(dataGridViewFacts[e.ColumnIndex, e.RowIndex]);
                dataGridViewFacts[e.ColumnIndex, e.RowIndex].Style.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void dataGridViewDrawings_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0 || dataGridViewDrawings.Rows[e.RowIndex].IsNewRow) return;

            var value = dataGridViewDrawings[e.ColumnIndex, e.RowIndex].Value;

            switch (e.ColumnIndex)
            {
                case 1:
                    {
                        newCellValue = value;
                        break;
                    }
                case 2:
                case 4:
                    {
                        float result;
                        if (!float.TryParse(value.ToString(), out result))
                        {
                            MessageBox.Show(Resources.BadValueMessage, "Error");
                            newCellValue = origCellValue;
                            dataGridViewDrawings[e.ColumnIndex, e.RowIndex].Value = origCellValue;
                        }
                        else
                        {
                            if (dataGridViewDrawings[e.ColumnIndex+1, e.RowIndex].Value.ToString() == "(none)")
                            {
                                dataGridViewDrawings[e.ColumnIndex + 1, e.RowIndex].Value = "Rough";
                            }
                            newCellValue = result;
                        }
                        break;
                    }
                case 3:
                case 5:
                    {
                        if (dataGridViewDrawings[e.ColumnIndex - 1, e.RowIndex].Value.ToString() == String.Empty)
                        {
                            dataGridViewDrawings[e.ColumnIndex - 1, e.RowIndex].Value = 0;
                        }
                        newCellValue = value;
                        break;
                    }
            }

            if (newCellValue.ToString() != origCellValue.ToString())
            {
                var name = dataGridViewDrawings[0, e.RowIndex].Value.ToString();
                _gameSave.EditDrawing(name, comboBoxSelectCP.SelectedIndex, e.ColumnIndex, newCellValue);
                if (e.ColumnIndex != 1)
                {
                    dataGridViewDrawings[1, e.RowIndex].Value = true;
                }
                else if (Convert.ToBoolean(newCellValue) == true)
                {
                    for (int i = 2; i <= 5; i++)
                    {
                        string cell_value = dataGridViewDrawings[i, e.RowIndex].Value.ToString();
                        if ((i % 2 == 0 && !String.IsNullOrWhiteSpace(cell_value)) ||
                            (i % 2 == 1 && cell_value != "(none)"))
                        {
                            _gameSave.EditDrawing(name, comboBoxSelectCP.SelectedIndex, i, cell_value);
                        }
                    }
                }
                _editedControls.AddUnique(dataGridViewDrawings[e.ColumnIndex, e.RowIndex]);
                dataGridViewDrawings[e.ColumnIndex, e.RowIndex].Style.BackColor = Color.LightGoldenrodYellow;

                ShowChangesWarning();
            }
        }

        private void dataGridViewWorld_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            string name = dataGridViewWorld[0, e.RowIndex].Value.ToString();

            newCellValue = dataGridViewWorld[e.ColumnIndex, e.RowIndex].Value;

            if (newCellValue.ToString() != origCellValue.ToString())
            {
                string property = "";
                if (_gameSave.saveType == SaveType.CaptainSpirit)
                {
                    switch (e.ColumnIndex)
                    {
                        case 1:
                            {
                                property = "bShouldBeLoaded";
                                break;
                            }
                        case 2:
                            {
                                property = "bShouldBeVisible";
                                break;
                            }
                        case 3:
                            {
                                property = "bShouldBlockOnLoad";
                                break;
                            }
                        case 4:
                            {
                                property = "bHasLoadedLevel";
                                break;
                            }
                        case 5:
                            {
                                property = "bIsVisible";
                                break;
                            }
                    }
                }
                else
                {
                    switch (e.ColumnIndex)
                    {
                        case 1:
                            {
                                property = "bShouldBeLoaded";
                                break;
                            }
                        case 2:
                            {
                                property = "bShouldBeVisible";
                                break;
                            }
                        case 3:
                            {
                                property = "bHasLoadedLevel";
                                break;
                            }
                        case 4:
                            {
                                property = "bIsVisible";
                                break;
                            }
                        case 5:
                            {
                                property = "bIsRequestingUnloadAndRemoval";
                                break;
                            }
                    }
                }

                _gameSave.EditPackageProperty(name, comboBoxSelectCP.SelectedIndex, property, Convert.ToBoolean(newCellValue));
                _editedControls.AddUnique(dataGridViewWorld[e.ColumnIndex, e.RowIndex]);
                dataGridViewWorld[e.ColumnIndex, e.RowIndex].Style.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void dataGridViewSeenPics_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0 || dataGridViewSeenPics.Rows[e.RowIndex].IsNewRow) return;

            newCellValue = dataGridViewSeenPics[e.ColumnIndex, e.RowIndex].Value;

            if (newCellValue.ToString() != origCellValue.ToString())
            {
                var name = dataGridViewSeenPics[0, e.RowIndex].Value.ToString();
                _gameSave.EditSeenPicture(name, comboBoxSelectCP.SelectedIndex, e.ColumnIndex, Convert.ToBoolean(newCellValue));

                if (_gameSave.saveType == SaveType.LIS)
                {
                    if (e.ColumnIndex != 1)
                    {
                        dataGridViewSeenPics[1, e.RowIndex].Value = true;
                    }
                    else if (Convert.ToBoolean(newCellValue) == true)
                    {
                        if (Convert.ToBoolean(dataGridViewSeenPics[2, e.RowIndex].Value) == true)
                        {
                            _gameSave.EditSeenPicture(name, comboBoxSelectCP.SelectedIndex, 2, true);
                        }
                        if (Convert.ToBoolean(dataGridViewSeenPics[3, e.RowIndex].Value) == true)
                        {
                            _gameSave.EditSeenPicture(name, comboBoxSelectCP.SelectedIndex, 3, true);
                        }
                    }
                }
                _editedControls.AddUnique(dataGridViewSeenPics[e.ColumnIndex, e.RowIndex]);
                dataGridViewSeenPics[e.ColumnIndex, e.RowIndex].Style.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }


        private void dataGridViewCollectibles_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var value = dataGridViewCollectibles[e.ColumnIndex, e.RowIndex].Value;

            if (String.IsNullOrWhiteSpace(value.ToString()))
            {
                newCellValue = "";
                dataGridViewCollectibles[e.ColumnIndex, e.RowIndex].Value = "";
            }
            else if (e.ColumnIndex == 1)
            {
                int result;
                if (!int.TryParse(value.ToString(), out result))
                {
                    MessageBox.Show(Resources.BadValueMessage, "Error");
                    newCellValue = origCellValue;
                    dataGridViewCollectibles[e.ColumnIndex, e.RowIndex].Value = origCellValue;
                }
                else
                {
                    newCellValue = result;
                }
            }
            else
            {
                newCellValue = value;
                if (String.IsNullOrWhiteSpace(dataGridViewCollectibles[1, e.RowIndex].Value.ToString()))
                {
                    dataGridViewCollectibles[1, e.RowIndex].Value = -1;
                }
            }

            if (newCellValue.ToString() != origCellValue.ToString())
            {
                var name = dataGridViewCollectibles[0, e.RowIndex].Value.ToString();
                _gameSave.EditCollectible(name, comboBoxSelectCP.SelectedIndex, e.ColumnIndex, newCellValue);
                _editedControls.AddUnique(dataGridViewCollectibles[e.ColumnIndex, e.RowIndex]);
                dataGridViewCollectibles[e.ColumnIndex, e.RowIndex].Style.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void dataGridViewObjectives_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            newCellValue = dataGridViewObjectives[e.ColumnIndex, e.RowIndex].Value.ToString();

            if (newCellValue.ToString() != origCellValue.ToString())
            {
                var name = dataGridViewObjectives[0, e.RowIndex].Value.ToString();
                _gameSave.EditObjective(name, comboBoxSelectCP.SelectedIndex, newCellValue.ToString());
                _editedControls.AddUnique(dataGridViewObjectives[0, e.RowIndex]);
                dataGridViewObjectives[0, e.RowIndex].Style.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void dataGridViewSeenMessages_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            newCellValue = dataGridViewSeenMessages[e.ColumnIndex, e.RowIndex].Value;

            if (newCellValue.ToString() != origCellValue.ToString())
            {
                var name = dataGridViewSeenMessages[0, e.RowIndex].Value.ToString();
                _gameSave.EditSeenMessage(name, comboBoxSelectCP.SelectedIndex, Convert.ToBoolean(newCellValue));
                _editedControls.AddUnique(dataGridViewSeenMessages[e.ColumnIndex, e.RowIndex]);
                dataGridViewSeenMessages[e.ColumnIndex, e.RowIndex].Style.BackColor = Color.LightGoldenrodYellow;
                ShowChangesWarning();
            }
        }

        private void textBoxMetricsCounters_TextChanged(object sender, EventArgs e)
        {
            var tb = (TextBox)sender;
            string[] info = tb.Tag.ToString().Split(new string[] { "::" }, 2, StringSplitOptions.RemoveEmptyEntries);
            uint value = 0;
            try
            {
                value = Convert.ToUInt32(tb.Text);
                tb.BackColor = Color.LightGoldenrodYellow;
                _editedControls.AddUnique(tb);
            }
            catch
            {
                tb.BackColor = Color.Red;
            }

            if (comboBoxSelectCP.SelectedIndex == 0)
            {
                _gameSave.Data["CurrentSubContextSaveData"].Value["MetricsSaveData"].Value["MetricsBySection"].Value[info[0]]["Counters"].Value[info[1]] = value;
            }
            else
            {
                _gameSave.Data["CheckpointHistory"].Value[comboBoxSelectCP.SelectedIndex]["MetricsSaveData"].Value["MetricsBySection"].Value[info[0]]["Counters"].Value[info[1]] = value;
            }
            
            ShowChangesWarning();
        }

        private void textBoxMetricsTime_TextChanged(object sender, EventArgs e)
        {
            var tb = (TextBox)sender;
            string[] info = tb.Tag.ToString().Split(new string[] { "::" }, 2, StringSplitOptions.RemoveEmptyEntries);
            float value = 0;
            try
            {
                value = Convert.ToSingle(tb.Text);
                tb.BackColor = Color.LightGoldenrodYellow;
                _editedControls.AddUnique(tb);
            }
            catch
            {
                tb.BackColor = Color.Red;
            }

            if (comboBoxSelectCP.SelectedIndex == 0)
            {
                _gameSave.Data["CurrentSubContextSaveData"].Value["MetricsSaveData"].Value["MetricsBySection"].Value[info[0]]["TimeCounters"].Value[info[1]] = value;
            }
            else
            {
                _gameSave.Data["CheckpointHistory"].Value[comboBoxSelectCP.SelectedIndex]["MetricsSaveData"].Value["MetricsBySection"].Value[info[0]]["TimeCounters"].Value[info[1]] = value;
            }

            
            ShowChangesWarning();
        }

        private void textBoxMetricsInteraction_TextChanged(object sender, EventArgs e)
        {
            var tb = (TextBox)sender;
            string[] info = tb.Tag.ToString().Split(new string[] { "::" }, 3, StringSplitOptions.RemoveEmptyEntries);
            uint value = 0;
            try
            {
                value = Convert.ToUInt32(tb.Text);
                tb.BackColor = Color.LightGoldenrodYellow;
                _editedControls.AddUnique(tb);
            }
            catch
            {
                tb.BackColor = Color.Red;
            }

            if (comboBoxSelectCP.SelectedIndex == 0)
            {
                _gameSave.Data["CurrentSubContextSaveData"].Value["MetricsSaveData"].Value["MetricsBySection"].Value[info[0]]["InteractionCounters"].Value[info[2]][info[1]].Value = value;
            }
            else
            {
                _gameSave.Data["CheckpointHistory"].Value[comboBoxSelectCP.SelectedIndex]["MetricsSaveData"].Value["MetricsBySection"].Value[info[0]]["InteractionCounters"].Value[info[2]][info[1]].Value = value;

            }
            ShowChangesWarning();
        }
        #endregion

        private void textBoxSavePath_TextChanged(object sender, EventArgs e)
        {
            ValidatePaths();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Text = $"Life is Strange 2 Savegame Editor v{Program.GetApplicationVersionStr()}";
            foreach (TabPage page in tabControlMain.TabPages)
            {
                tabControlMain.SelectedTab = page;
            }
            tabControlMain.SelectedTab = tabPageGeneral;

            DetectSavePath();
            textBoxSavePath.Text = _settingManager.Settings.SavePath;

            labelChangesWarning.Visible = false; 
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _settingManager.SaveSettings();

            if (_gameSave != null && !_gameSave.SaveChangesSaved)
            {
                DialogResult answer = MessageBox.Show(Resources.UnsavedEditsWarningMessage,
                    "Savegame Editor", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (answer == DialogResult.Yes)
                {
                    e.Cancel = false;
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else e.Cancel = false;
        }

        private void DetectSavePath()
        {
            try
            {
                _steamIdFolders = Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\AppData\Local\Dontnod\").ToList<string>();
            }
            catch
            {

            }

            if (String.IsNullOrEmpty(_settingManager.Settings.SavePath))
            {
                if (_steamIdFolders.Count >= 1)
                {
                    bool found = false;

                    foreach (var folder in _steamIdFolders)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            if (File.Exists(_steamIdFolders[0].ToString() + @"\LIS2\Saved\SaveGames\GameSave_Slot" + i + ".sav"))
                            {
                                textBoxSavePath.Text = _steamIdFolders[0].ToString() + @"\LIS2\Saved\SaveGames\GameSave_Slot" + i + ".sav";
                                _settingManager.Settings.SavePath = textBoxSavePath.Text;
                                found = true;
                                break;
                            }
                        }

                        if (!found && File.Exists(_steamIdFolders[0].ToString() + @"\CaptainSpirit\Saved\SaveGames\GameSave_Slot0.sav"))
                        {
                            textBoxSavePath.Text = _steamIdFolders[0].ToString() + @"\CaptainSpirit\Saved\SaveGames\GameSave_Slot0.sav";
                            _settingManager.Settings.SavePath = textBoxSavePath.Text;
                            found = true;
                        }
                        if (found)
                        {
                            break;
                        }
                    }
                    if (!found)
                    {
                        textBoxSavePath.Text = "Auto-detection failed! Please select the path manually.";
                    }
                    
                }
                else
                {
                    textBoxSavePath.Text = "Auto-detection failed! Please select the path manually.";
                }
            }
            else
            {
                textBoxSavePath.Text = _settingManager.Settings.SavePath;
            }
        }

        private void ValidatePaths()
        {
            bool success = false;
            try
            {
                success = File.Exists(textBoxSavePath.Text) && Path.GetExtension(textBoxSavePath.Text) == ".sav";
            }
            catch
            {

            }
            if (success)
            {
                textBoxSavePath.BackColor = SystemColors.Window;
                _settingManager.Settings.SavePath = textBoxSavePath.Text;
                buttonLoad.Enabled = true;
                buttonSaveEdits.Enabled = false;
                tabControlMain.Enabled = false;
                comboBoxSelectCP.Enabled = false;
                labelChangesWarning.Text = "Save file changed! Press 'Load' to update.";
                labelChangesWarning.Visible = true;
            }
            else
            {
                textBoxSavePath.BackColor = Color.Red;
                buttonLoad.Enabled = false;
                buttonSaveEdits.Enabled = false;
                tabControlMain.Enabled = false;
                comboBoxSelectCP.Enabled = false;
            }
        }

        private void comboBoxSelectCP_SelectedIndexChanged(object sender, EventArgs e)
        {
            SaveLoading = true;

            int index = comboBoxSelectCP.SelectedIndex;

            //General tab
            if (_gameSave.saveType == SaveType.CaptainSpirit)
            { 
                comboBoxCPName.SelectedItem = _gameSave.Data["CheckpointName"].Value;
                textBoxSubContextID.Text = _gameSave.Data["CurrentSubContextSaveData"].Value["SubContextId"].Value;
            }
            else if (index == 0)
            {
                comboBoxCPName.SelectedItem = _gameSave.Data["CurrentSubContextSaveData"].Value["CheckpointName"].Value;
                textBoxSubContextID.Text = _gameSave.Data["CurrentSubContextSaveData"].Value["SubContextId"].Value;
            }
            else
            {
                comboBoxCPName.SelectedItem = _gameSave.Data["CheckpointHistory"].Value[index]["CheckpointName"].Value;
                textBoxSubContextID.Text = _gameSave.Data["CheckpointHistory"].Value[index]["SubContextId"].Value;

            }
            textBoxMapName.Text = _gameSave.Data["MapName"].Value;
            textBoxSubContextPath.Text = _gameSave.Data["CurrentSubContextPathName"].Value;
            dateTimePickerSaveTime.Value = _gameSave.Data["SaveTime"].Value["DateTime"];

            UpdatePlayerInfo(index);
            UpdateDanielInfo(index);
            UpdateAICallInfo(index);
            UpdateStats(index);
            UpdateInventoryGrids(index);
            UpdateSeenNotifsGrid(index);
            UpdateSeenTutosGrid(index);
            UpdateDrawingsGrid(index);
            UpdateAllFactsGrid(index);
            UpdateWorldGrid(index);
            GenerateMetrics(index);
            UpdateSeenPicturesGrid(index);
            UpdateCollectiblesGrid(index);
            UpdateObjectivesGrid(index);
            UpdateSeenMessagesGrid(index);

            SaveLoading = false;
            ResetEditedControls();
        }

        private void ShowChangesWarning()
        {
            _gameSave.SaveChangesSaved = false;
            labelChangesWarning.Text = "Press 'Save' to write changes to the save file.";
            labelChangesWarning.Visible = true;
        }
        
        private void ResetEditedControls()
        {
            foreach (var cnt in _editedControls)
            {
                if (cnt is DataGridViewCell)
                {
                    cnt.Style.BackColor = Color.White;
                }
                else if (cnt is TextBox)
                {
                    cnt.BackColor = SystemColors.Window;
                }
                else if (cnt is Panel || cnt is GroupBox || cnt is CheckBox)
                {
                    cnt.BackColor = Color.Transparent;
                }
                else
                {
                    cnt.BackColor = Color.White;
                }
            }
            _editedControls.Clear();
        }

        private void ClearGroupBox(GroupBox gb)
        {
            foreach (Control cnt in gb.Controls)
            {
                if (cnt is TextBox)
                {
                    cnt.Text = "";
                }
                else if (cnt is Panel)
                {
                    ((ComboBox)cnt.Controls[0]).SelectedIndex = -1;
                }
                else if (cnt is CheckBox)
                {
                    ((CheckBox)cnt).Checked = false;
                }
            }
        }
    }
}