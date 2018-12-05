﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using lis2_save_editor.Properties;

namespace lis2_save_editor
{
    public partial class LevelEditForm : Form
    {
        public LevelEditForm()
        {
            InitializeComponent();
        }

        public Dictionary<string, dynamic> level = null;

        private LevelObject level_info
        {
            get
            {
                return GameInfo.LIS2_Levels.Find(x => x.Name == level["LevelName"].Value);
            }
        }

        public bool changesMade = false;

        private void LevelEditForm_Load(object sender, EventArgs e)
        {
            GenerateInteractions();
            UpdatePOITable();
        }

        #region Building

        private void GenerateInteractions()
        {
            flowLayoutPanelInteractions.Controls.Clear();

            List<dynamic> root = level["InteractionsSaveData"].Value["InteractionActors"].Value;

            int lbl_coord = 50, max_lbl_width = 0, max_gbox_height = 0;

            Regex reg = new Regex(@"(?<=_)C\d+_.*");

            foreach (var obj in level_info.Interactions)
            {
                var gbox = new GroupBox();
                gbox.AutoSize = true;
                gbox.AutoSizeMode = AutoSizeMode.GrowOnly;
                gbox.Text = obj.Name;

                //size crutch
                var text_lbl = new Label();
                text_lbl.AutoSize = true;
                text_lbl.Text = gbox.Text;
                text_lbl.Visible = false;
                gbox.Controls.Add(text_lbl);
                gbox.MinimumSize = new Size(text_lbl.Width + 20, gbox.Height);

                var actor_ind = root.FindIndex(1, x => x["InteractionActorName"].Value == obj.Name);

                var cbActive= new CheckBox();
                cbActive.Name = "Active";
                cbActive.Text = "Active";
                cbActive.Location = new Point(6, 20);
                cbActive.AutoSize = true;
                cbActive.Checked = (actor_ind != -1);
                cbActive.CheckedChanged += new EventHandler(checkBoxIntObject_CheckedChanged);
                gbox.Controls.Add(cbActive);

                var cbEnabled = new CheckBox();
                cbEnabled.Text = "Enabled";
                cbEnabled.Location = new Point(75, 20);
                cbEnabled.AutoSize = true;
                cbEnabled.Checked = actor_ind == -1 ? false : root[actor_ind]["bIsEnable"].Value;
                cbEnabled.CheckedChanged += new EventHandler(checkBoxIntObject_CheckedChanged);
                gbox.Controls.Add(cbEnabled);

                var cbDestroyed = new CheckBox();
                cbDestroyed.Text = "Destroyed";
                cbDestroyed.Location = new Point(175, 20);
                cbDestroyed.AutoSize = true;
                cbDestroyed.Checked = actor_ind == -1 ? false : root[actor_ind]["bIsConsideredDestroyed"].Value;
                cbDestroyed.CheckedChanged += new EventHandler(checkBoxIntObject_CheckedChanged);
                gbox.Controls.Add(cbDestroyed);

                foreach (var inter in obj.ClassicInteractions)
                {
                    var lbl = new Label();
                    lbl.AutoSize = true;
                    lbl.Location = new Point(3, lbl_coord);
                    lbl.Text = reg.Match(inter.Value).Value;
                    gbox.Controls.Add(lbl);

                    if (lbl.Width > max_lbl_width)
                    {
                        max_lbl_width = lbl.Width;
                    }

                    var inter_ind = -1;
                    List<dynamic> int_list = null;
                    if (actor_ind != -1)
                    {
                        int_list = root[actor_ind]["ClassicInteractions"].Value;
                        inter_ind = int_list.FindIndex(1, x => x["InteractionGuid"].Value["Guid"] == inter.Key);
                    }

                    var tb = new TextBox();
                    tb.Location = new Point(lbl.Location.X + 3, lbl.Location.Y);
                    tb.Name = "tb" + lbl.Text;
                    tb.Tag = obj.Name + "::" + inter.Key.ToString();
                    tb.Size = new Size(60, 20);
                    tb.Text = (actor_ind == -1 || inter_ind == -1) ? "" : int_list[inter_ind]["InteractionExecutionCount"].Value.ToString();
                    tb.TextChanged += new EventHandler(textBoxInteraction_CheckedChanged);
                    gbox.Controls.Add(tb);
                    lbl_coord += 26;
                }

                lbl_coord = 50;

                foreach (var tb in gbox.Controls.OfType<TextBox>())
                {
                    tb.Location = new Point(tb.Location.X + max_lbl_width, tb.Location.Y);
                }

                flowLayoutPanelInteractions.Controls.Add(gbox);

                if(gbox.Height > max_gbox_height)
                {
                    max_gbox_height = gbox.Height;
                }
                
                max_lbl_width = 0;
            }


        }

        private void UpdatePOITable()
        {
            dataGridViewPOIs.Columns.Clear();
            dataGridViewPOIs.DataSource = BuildPOITable().DefaultView;

            dataGridViewPOIs.Columns[1].FillWeight = 20;
            dataGridViewPOIs.Columns[2].FillWeight = 20;
            dataGridViewPOIs.Columns[3].FillWeight = 20;
        }

        private DataTable BuildPOITable()
        {
            DataTable t = new DataTable();
            t.Columns.Add("Name");
            t.Columns.Add("Active", typeof(bool));
            t.Columns.Add("Enabled", typeof(bool));
            t.Columns.Add("Remaining cooldown");

            List<dynamic> target = level["PointsOfInterestSaveData"].Value;

            foreach (var poi in level_info.PointsOfInterest)
            {
                object[] row = new object[t.Columns.Count];
                row[0] = poi;
                
                int index = target.FindIndex(1, x => x["PointOfInterestActorName"].Value == poi);
                if (index != -1)
                {
                    row[1] = true;
                    row[2] = target[index]["bIsPointOfInterestEnabled"].Value;
                    row[3] = target[index]["RemainingCoolDownTime"].Value;
                }
                else
                {
                    row[1] = false;
                    row[2] = false;
                    row[3] = 0;
                }
                t.Rows.Add(row);
            }
            return t;
        }
        #endregion

        #region Editing

        private void checkBoxIntObject_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            List<dynamic> root = level["InteractionsSaveData"].Value["InteractionActors"].Value;
            var ind = root.FindIndex(1, x => x["InteractionActorName"].Value == cb.Parent.Text);
            bool state = cb.Checked;
            switch (cb.Text)
            {
                case "Active":
                    {
                        if (state == false)
                        {
                            root.RemoveAt(ind);
                        }
                        else
                        {
                            AddInteractionActor(cb.Parent.Text);
                        }
                        break;
                    }
                case "Enabled":
                    {
                        ((CheckBox)cb.Parent.Controls["Active"]).Checked = true;
                        ind = root.FindIndex(1, x => x["InteractionActorName"].Value == cb.Parent.Text);
                        root[ind]["bIsEnable"].Value = state;
                        break;
                    }
                case "Destroyed":
                    {
                        ((CheckBox)cb.Parent.Controls["Active"]).Checked = true;
                        ind = root.FindIndex(1, x => x["InteractionActorName"].Value == cb.Parent.Text);
                        root[ind]["bIsConsideredDestroyed"].Value = state;
                        break;
                    }
                default:
                    {
                        throw new Exception("Unknown checkbox");
                    }
            }
            cb.BackColor = Color.LightGoldenrodYellow;
            changesMade = true;
        }

        private void textBoxInteraction_CheckedChanged(object sender, EventArgs e)
        {
            TextBox tb = (TextBox)sender;
            string[] info = tb.Tag.ToString().Split(new string[] { "::" }, 2, StringSplitOptions.RemoveEmptyEntries);

            if (String.IsNullOrEmpty(tb.Text))
            {
                List<dynamic> root = level["InteractionsSaveData"].Value["InteractionActors"].Value;
                var ind1 = root.FindIndex(1, x => x["InteractionActorName"].Value == info[0]);
                List<dynamic> int_list = root[ind1]["ClassicInteractions"].Value;
                var ind2 = int_list.FindIndex(1, x => x["InteractionGuid"].Value["Guid"].ToString() == info[1]);
                int_list.RemoveAt(ind2);
                tb.BackColor = Color.LightGoldenrodYellow;
            }
            else
            {
                int value = 0;
                try
                {
                    value = Convert.ToInt32(tb.Text);
                    tb.BackColor = Color.LightGoldenrodYellow;
                }
                catch
                {
                    tb.BackColor = Color.Red;
                }

                List<dynamic> root = level["InteractionsSaveData"].Value["InteractionActors"].Value;
                var ind1 = root.FindIndex(1, x => x["InteractionActorName"].Value == info[0]);
                if (ind1 == -1)
                {
                    AddInteractionActor(info[0]);
                    ind1 = root.Count - 1;
                }

                List<dynamic> int_list = root[ind1]["ClassicInteractions"].Value;
                var ind2 = int_list.FindIndex(1, x => x["InteractionGuid"].Value["Guid"].ToString() == info[1]);

                if (ind2 == -1)
                {
                    Guid guid = new Guid(info[1]);
                    string inter_name = level_info.Interactions.Find(x => x.Name == info[0]).ClassicInteractions[guid];
                    Dictionary<string, dynamic> new_inter = new Dictionary<string, dynamic>()
                    {
                        {
                            "InteractionNameForDebug", new NameProperty()
                            {
                                Name = "InteractionNameForDebug",
                                Type = "NameProperty",
                                Value = inter_name
                            }
                        },
                        {
                            "InteractionGuid", new StructProperty()
                            {
                                Name = "InteractionGuid",
                                Type = "StructProperty",
                                ElementType = "Guid",
                                Value = new Dictionary<string, dynamic>()
                                {
                                    { "Guid",  guid}
                                }
                            }
                        },
                        {
                            "InteractionExecutionCount", new IntProperty()
                            {
                                Name = "InteractionExecutionCount",
                                Type = "IntProperty",
                                Value = 0
                            }
                        }
                    };
                    int_list.Add(new_inter);
                    ind2 = int_list.Count - 1;
                }

                int_list[ind2]["InteractionExecutionCount"].Value = value;
            }

            changesMade = true;
        }

        private void AddInteractionActor(string name)
        {
            List<dynamic> root = level["InteractionsSaveData"].Value["InteractionActors"].Value;
            Dictionary<string, dynamic> new_actor = new Dictionary<string, dynamic>()
            {
                { "InteractionActorName", new NameProperty
                    {
                        Name = "InteractionActorName",
                        Type = "NameProperty",
                        Value = name
                    }
                },
                {
                    "bIsEnable", new BoolProperty
                    {
                        Name = "bIsEnable",
                        Type = "BoolProperty",
                        Value = true
                    }
                },
                {
                    "bIsConsideredDestroyed", new BoolProperty
                    {
                        Name = "bIsConsideredDestroyed",
                        Type = "BoolProperty",
                        Value = false
                    }
                },
                {
                    "ClassicInteractions", new ArrayProperty
                    {
                        Name = "ClassicInteractions",
                        Type = "ArrayProperty",
                        ElementType = "StructProperty",
                        Value = new List<dynamic>
                        {
                            new  Dictionary<string, object>()
                            {
                                { "struct_name", "ClassicInteractions" },
                                {"struct_type", "StructProperty" },
                                {"struct_length", 0 },
                                {"struct_eltype", "LIS2InteractionSaveData" },
                                {"struct_unkbytes", new byte[17] }
                            }
                        }
                    }
                },
                {
                    "DanielInteractions", new ArrayProperty
                    {
                        Name = "DanielInteractions",
                        Type = "ArrayProperty",
                        ElementType = "StructProperty",
                        Value = new List<dynamic>
                        {
                            new  Dictionary<string, object>()
                            {
                                { "struct_name", "DanielInteractions" },
                                {"struct_type", "StructProperty" },
                                {"struct_length", 0 },
                                {"struct_eltype", "LIS2InteractionSaveData" },
                                {"struct_unkbytes", new byte[17] }
                            }
                        }
                    }
                }
            };
            root.Add(new_actor);
        }

        object origCellValue, newCellValue;

        private void dataGridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            var grid = ((DataGridView)sender);
            if (e.ColumnIndex == 0)
            {
                e.Cancel = true;
                return;
            }
            origCellValue = grid[e.ColumnIndex, e.RowIndex].Value;
        }

        private void dataGridViewPOIs_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 3)
            {
                float result;
                if (!float.TryParse(dataGridViewPOIs[3, e.RowIndex].Value.ToString(), out result))
                {
                    MessageBox.Show(Resources.BadValueMessage, "Error");
                    newCellValue = origCellValue;
                    dataGridViewPOIs[e.ColumnIndex, e.RowIndex].Value = origCellValue;
                }
                else
                {
                    newCellValue = result;
                }
            }
            else
            {
                newCellValue = dataGridViewPOIs[e.ColumnIndex, e.RowIndex].Value;
            }

            if (newCellValue.ToString() != origCellValue.ToString())
            {
                var name = dataGridViewPOIs[0, e.RowIndex].Value.ToString();
                EditPOIValue(name, e.ColumnIndex, newCellValue);
                dataGridViewPOIs[e.ColumnIndex, e.RowIndex].Style.BackColor = Color.LightGoldenrodYellow;

                if (e.ColumnIndex != 1)
                {
                    dataGridViewPOIs[1, e.RowIndex].Value = true;
                    dataGridViewPOIs[2, e.RowIndex].Value = true;
                }
                else if (Convert.ToBoolean(newCellValue) == true)
                {
                    EditPOIValue(name, 2, dataGridViewPOIs[2, e.RowIndex].Value);
                    EditPOIValue(name, 3, dataGridViewPOIs[2, e.RowIndex].Value);
                }
            }
        }

        private void EditPOIValue(string name, int colIndex, object value)
        {
            List<dynamic> target = level["PointsOfInterestSaveData"].Value;
            int index = target.FindIndex(1, x => x["PointOfInterestActorName"].Value == name);

            if (index == -1) //add new item
            {
                Dictionary<string, object> new_item = new Dictionary<string, object>()
                {
                    {
                        "PointOfInterestActorName", new NameProperty()
                        {
                            Name = "PointOfInterestActorName",
                            Type = "NameProperty",
                            Value = name
                        }
                    },
                    {
                        "bIsPointOfInterestEnabled", new BoolProperty()
                        {
                            Name = "bIsPointOfInterestEnabled",
                            Type = "BoolProperty",
                            Value = colIndex == 1 ? true : Convert.ToBoolean(value)
                        }
                    },
                    {
                        "RemainingCoolDownTime", new FloatProperty()
                        {
                            Name = "RemainingCoolDownTime",
                            Type = "FloatProperty",
                            Value = colIndex == 1 ? 0 : Convert.ToSingle(value)
                        }
                    }
                };
                target.AddUnique(new_item);
            }
            else
            {
                if (colIndex == 1 && Convert.ToBoolean(value) == false) //Remove POI
                {
                    target.RemoveAt(index);
                }
                else
                {
                    switch (colIndex)
                    {
                        case 2:
                            {
                                target[index]["bIsPointOfInterestEnabled"].Value = Convert.ToBoolean(value);
                                break;
                            }
                        case 3:
                            {
                                target[index]["RemainingCoolDownTime"].Value = Convert.ToSingle(value);
                                break;
                            }
                    }
                }
            }
            changesMade = true;
        }

        #endregion

        private void LevelEditForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            dataGridViewPOIs.EndEdit();
        }

        private void LevelEditForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 27)
            {
                Close();
            }
        }
    }
}
