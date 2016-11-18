﻿using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Windows.Forms;

namespace pk3DS
{
    public partial class SMTE : Form
    {
        private readonly trdata7[] Trainers;
        private string[][] AltForms;
        private static readonly Random rand = new Random();
        internal static uint rnd32()
        {
            return (uint)rand.Next(1 << 30) << 2 | (uint)rand.Next(1 << 2);
        }
        private int index = -1;
        private PictureBox[] pba;

        private readonly string[] trdatapaths = Directory.GetFiles("trdata");
        private readonly string[] trpokepaths = Directory.GetFiles("trpoke");
        private readonly string[] abilitylist = Main.getText(TextName.AbilityNames);
        private readonly string[] movelist = Main.getText(TextName.MoveNames);
        private readonly string[] itemlist = Main.getText(TextName.ItemNames);
        private readonly string[] specieslist = Main.getText(TextName.SpeciesNames);
        private readonly string[] types = Main.getText(TextName.Types);
        private readonly string[] natures = Main.getText(TextName.Natures);
        private readonly string[] forms = Enumerable.Range(0, 1000).Select(i => i.ToString("000")).ToArray();
        private string[] trName = Main.getText(TextName.TrainerNames);
        private readonly string[] trClass = Main.getText(TextName.TrainerClasses);
        private readonly string[] trText = Main.getText(TextName.TrainerText);

        public SMTE()
        {
            InitializeComponent();

            mnuView.Click += clickView;
            mnuSet.Click += clickSet;
            mnuDelete.Click += clickDelete;
            Trainers = new trdata7[trdatapaths.Length];
            Setup();
            foreach (var pb in pba)
                pb.Click += clickSlot;

            CB_TrainerID.SelectedIndex = 0;
        }

        private int getSlot(object sender)
        {
            var send = ((sender as ToolStripItem)?.Owner as ContextMenuStrip)?.SourceControl ?? sender as PictureBox;
            return Array.IndexOf(pba, send);
        }
        private void clickSlot(object sender, EventArgs e)
        {
            switch (ModifierKeys)
            {
                case Keys.Control: clickView(sender, e); break;
                case Keys.Shift: clickSet(sender, e); break;
                case Keys.Alt: clickDelete(sender, e); break;
            }
        }
        private void clickView(object sender, EventArgs e)
        {
            int slot = getSlot(sender);
            if (pba[slot].Image == null)
            { SystemSounds.Exclamation.Play(); return; }
            
            // Load the PKM
            var pk = Trainers[index].Pokemon[slot];
            if (pk.Species != 0)
            {
                try { populateFieldsTP7(pk); }
                catch { }
                // Visual to display what slot is currently loaded.
                getSlotColor(slot, Properties.Resources.slotView);
            }
            else
                SystemSounds.Exclamation.Play();
        }
        private void clickSet(object sender, EventArgs e)
        {
            int slot = getSlot(sender);
            if (CB_Pokemon.SelectedIndex == 0)
            { Util.Alert("Can't set empty slot."); return; }

            var pk = prepareTP7();
            var tr = Trainers[index];
            if (slot < tr.NumPokemon)
                tr.Pokemon[slot] = pk;
            else
            {
                tr.Pokemon.Add(pk);
                slot = tr.Pokemon.Count - 1;
                Trainers[index].NumPokemon = (int)(++NUD_NumPoke.Value);
            }

            getQuickFiller(pba[slot], pk);
            getSlotColor(slot, Properties.Resources.slotSet);
        }
        private void clickDelete(object sender, EventArgs e)
        {
            int slot = getSlot(sender);

            if (slot < Trainers[index].NumPokemon)
            {
                Trainers[index].Pokemon.RemoveAt(slot);
                Trainers[index].NumPokemon = (int)(--NUD_NumPoke.Value);
            }

            populateTeam(Trainers[index]);
            getSlotColor(slot, Properties.Resources.slotDel);
        }

        private void populateTeam(trdata7 tr)
        {
            for (int i = 0; i < tr.NumPokemon; i++)
                getQuickFiller(pba[i], tr.Pokemon[i]);
            for (int i = tr.NumPokemon; i < 6; i++)
                pba[i].Image = null;
        }

        private void getSlotColor(int slot, Image color)
        {
            foreach (PictureBox t in pba)
                t.BackgroundImage = null;

            pba[slot].BackgroundImage = color;
        }
        private static void getQuickFiller(PictureBox pb, trpoke7 pk)
        {
            Bitmap rawImg = Util.getSprite(pk.Species, pk.Form, pk.Gender, pk.Item, pk.Shiny);
            pb.Image = Util.scaleImage(rawImg, 2);
        }

        // Top Level Functions
        private void refreshFormAbility(object sender, EventArgs e)
        {
            if (index < 0)
                return;
            pkm.Form = CB_Forme.SelectedIndex;
            refreshPKMSlotAbility();
        }
        private void refreshSpeciesAbility(object sender, EventArgs e)
        {
            if (index < 0)
                return;
            pkm.Species = (ushort)CB_Pokemon.SelectedIndex;
            Personal.setForms(CB_Pokemon.SelectedIndex, CB_Forme, AltForms);
            refreshPKMSlotAbility();
        }
        private void refreshPKMSlotAbility()
        {
            int previousAbility = CB_Ability.SelectedIndex;

            int species = CB_Pokemon.SelectedIndex;
            int formnum = CB_Forme.SelectedIndex;
            species = Main.SpeciesStat[species].FormeIndex(species, formnum);

            CB_Ability.Items.Clear();
            CB_Ability.Items.Add("Any (1 or 2)");
            CB_Ability.Items.Add(abilitylist[Main.SpeciesStat[species].Abilities[0]] + " (1)");
            CB_Ability.Items.Add(abilitylist[Main.SpeciesStat[species].Abilities[1]] + " (2)");
            CB_Ability.Items.Add(abilitylist[Main.SpeciesStat[species].Abilities[2]] + " (H)");

            CB_Ability.SelectedIndex = previousAbility;
        }
        
        private void Setup()
        {
            AltForms = forms.Select(f => Enumerable.Range(0, 100).Select(i => i.ToString()).ToArray()).ToArray();

            Array.Resize(ref trName, trdatapaths.Length);
            CB_TrainerID.Items.Clear();
            for (int i = 0; i < trdatapaths.Length; i++)
                CB_TrainerID.Items.Add(string.Format("{1} - {0}", i.ToString("000"), trName[i] ?? "UNKNOWN"));

            CB_Trainer_Class.Items.Clear();
            for (int i = 0; i < trClass.Length; i++)
                CB_Trainer_Class.Items.Add(string.Format("{1} - {0}", i.ToString("000"), trClass[i]));

            Trainers[0] = new trdata7();

            for (int i = 1; i < trdatapaths.Length; i++)
            {
                Trainers[i] = new trdata7(File.ReadAllBytes(trdatapaths[i]), File.ReadAllBytes(trpokepaths[i]))
                {
                    Name = trName[i],
                    ID = i
                };
            }

            specieslist[0] = "---";
            abilitylist[0] = itemlist[0] = movelist[0] = "(None)";
            pba = new[] { PB_Team1, PB_Team2, PB_Team3, PB_Team4, PB_Team5, PB_Team6 };
            
            CB_Pokemon.Items.Clear();
            foreach (string s in specieslist)
                CB_Pokemon.Items.Add(s);

            CB_Move1.Items.Clear();
            CB_Move2.Items.Clear();
            CB_Move3.Items.Clear();
            CB_Move4.Items.Clear();
            foreach (string s in movelist)
            {
                CB_Move1.Items.Add(s);
                CB_Move2.Items.Add(s);
                CB_Move3.Items.Add(s);
                CB_Move4.Items.Add(s);
            }

            CB_HPType.DataSource = types.Skip(1).Take(16).ToArray();
            CB_HPType.SelectedIndex = 0;

            CB_Nature.Items.Clear();
            foreach (string s in natures)
                CB_Nature.Items.Add(s);

            CB_Item.Items.Clear();
            foreach (string s in itemlist)
                CB_Item.Items.Add(s);
                
            CB_Gender.Items.Clear();
            CB_Gender.Items.Add("- / G/Random");
            CB_Gender.Items.Add("♂ / M");
            CB_Gender.Items.Add("♀ / F");

            CB_Forme.Items.Add("");

            CB_Pokemon.SelectedIndex = 0;
            CB_Item_1.Items.Clear();
            CB_Item_2.Items.Clear();
            CB_Item_3.Items.Clear();
            CB_Item_4.Items.Clear();
            CB_Prize.Items.Clear();
            foreach (string s in itemlist)
            {
                CB_Item_1.Items.Add(s);
                CB_Item_2.Items.Add(s);
                CB_Item_3.Items.Add(s);
                CB_Item_4.Items.Add(s);
                CB_Prize.Items.Add(s);
            }

            CB_Money.Items.Clear();
            for (int i = 0; i < 256; i++)
            { CB_Money.Items.Add(i.ToString()); }

            CB_Battle_Type.Items.Clear();
            CB_Battle_Type.Items.Add("Single");
            CB_Battle_Type.Items.Add("Double");
            CB_Battle_Type.Items.Add("Royal");

            CB_TrainerID.SelectedIndex = 0;
            index = 0;
            pkm = new trpoke7();
            populateFieldsTP7(pkm);
            SystemSounds.Asterisk.Play();
        }

        private void changeTrainerIndex(object sender, EventArgs e)
        {
            saveEntry();
            loadEntry();
        }
        private void saveEntry()
        {
            if (index < 0)
                return;
            var tr = Trainers[index];
            prepareTR7(tr);
            byte[] trdata;
            byte[] trpoke;
            tr.Write(out trdata, out trpoke);
            File.WriteAllBytes(trdatapaths[index], trdata);
            File.WriteAllBytes(trpokepaths[index], trpoke);
            trName[index] = TB_TrainerName.Text;
        }
        private void loadEntry()
        {
            index = CB_TrainerID.SelectedIndex;
            var tr = Trainers[index];

            TB_TrainerName.Text = trName[index];

            populateFieldsTD7(tr);
        }

        private trpoke7 pkm;
        private void populateFieldsTP7(trpoke7 pk)
        {
            pkm = pk.Clone();

            int spec = pkm.Species, form = pkm.Form;

            CB_Pokemon.SelectedIndex = spec;
            CB_Forme.SelectedIndex = form;
            CB_Ability.SelectedIndex = pkm.Ability;
            CB_Item.SelectedIndex = pkm.Item;
            CHK_Shiny.Checked = pkm.Shiny;
            CB_Gender.SelectedIndex = pkm.Gender;

            CB_Move1.SelectedIndex = pkm.Move1;
            CB_Move2.SelectedIndex = pkm.Move2;
            CB_Move3.SelectedIndex = pkm.Move3;
            CB_Move4.SelectedIndex = pkm.Move4;

            updatingStats = true;
            CB_Nature.SelectedIndex = pkm.Nature;
            NUD_Level.Value = Math.Min(NUD_Level.Maximum, pkm.Level);

            TB_HPIV.Text = pkm.IV_HP.ToString();
            TB_ATKIV.Text = pkm.IV_ATK.ToString();
            TB_DEFIV.Text = pkm.IV_DEF.ToString();
            TB_SPAIV.Text = pkm.IV_SPA.ToString();
            TB_SPEIV.Text = pkm.IV_SPE.ToString();
            TB_SPDIV.Text = pkm.IV_SPD.ToString();

            TB_HPEV.Text = pkm.EV_HP.ToString();
            TB_ATKEV.Text = pkm.EV_ATK.ToString();
            TB_DEFEV.Text = pkm.EV_DEF.ToString();
            TB_SPAEV.Text = pkm.EV_SPA.ToString();
            TB_SPEEV.Text = pkm.EV_SPE.ToString();
            TB_SPDEV.Text = pkm.EV_SPD.ToString();
            updatingStats = false;
            updateStats(null, null);
        }
        private trpoke7 prepareTP7()
        {
            var pk = pkm.Clone();
            pk.Species = (ushort)CB_Pokemon.SelectedIndex;
            pk.Form = CB_Forme.SelectedIndex;
            pk.Level = (byte)NUD_Level.Value;
            pk.Ability = CB_Ability.SelectedIndex;
            pk.Item = (ushort) CB_Item.SelectedIndex;
            pk.Shiny = CHK_Shiny.Checked;
            pk.Nature = CB_Nature.SelectedIndex;
            pk.Gender = CB_Gender.SelectedIndex;

            pk.Move1 = (ushort)CB_Move1.SelectedIndex;
            pk.Move2 = (ushort)CB_Move2.SelectedIndex;
            pk.Move3 = (ushort)CB_Move3.SelectedIndex;
            pk.Move4 = (ushort)CB_Move4.SelectedIndex;

            pk.IV_HP = Util.ToInt32(TB_HPIV);
            pk.IV_ATK = Util.ToInt32(TB_ATKIV);
            pk.IV_DEF = Util.ToInt32(TB_DEFIV);
            pk.IV_SPA = Util.ToInt32(TB_SPAIV);
            pk.IV_SPE = Util.ToInt32(TB_SPEIV);
            pk.IV_SPD = Util.ToInt32(TB_SPDIV);

            pk.EV_HP = Util.ToInt32(TB_HPEV);
            pk.EV_ATK = Util.ToInt32(TB_ATKEV);
            pk.EV_DEF = Util.ToInt32(TB_DEFEV);
            pk.EV_SPA = Util.ToInt32(TB_SPAEV);
            pk.EV_SPE = Util.ToInt32(TB_SPEEV);
            pk.EV_SPD = Util.ToInt32(TB_SPDEV);

            return pk;
        }
        private void populateFieldsTD7(trdata7 tr)
        {
            // Load Trainer Data
            CB_Trainer_Class.SelectedIndex = tr.TrainerClass;
            NUD_NumPoke.Value = tr.NumPokemon;
            populateTeam(tr);
        }
        private void prepareTR7(trdata7 tr)
        {
            tr.TrainerClass = (byte)CB_Trainer_Class.SelectedIndex;
            tr.NumPokemon = (byte)NUD_NumPoke.Value;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            saveEntry();
            base.OnFormClosing(e);
        }

        // Dumping
        private void DumpTxt(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.FileName = "Trainers.txt";
                if (sfd.ShowDialog() != DialogResult.OK)
                    return;
                var sb = new StringBuilder();
                foreach (var Trainer in Trainers)
                    sb.Append(getTrainerString(Trainer));
                File.WriteAllText(sfd.FileName, sb.ToString());
            }
        }
        private string getTrainerString(trdata7 tr)
        {
            var sb = new StringBuilder();
            sb.AppendLine("======");
            sb.AppendLine($"{tr.ID} - {trClass[tr.TrainerClass]} {tr.Name}");
            sb.AppendLine("======");
            sb.AppendLine($"Pokemon: {tr.NumPokemon}");
            for (int i = 0; i < tr.NumPokemon; i++)
            {
                if (tr.Pokemon[i].Shiny)
                    sb.Append("Shiny ");
                sb.Append(specieslist[tr.Pokemon[i].Species]);
                sb.Append($" (Lv. {tr.Pokemon[i].Level}) ");
                if (tr.Pokemon[i].Item > 0)
                    sb.Append($"@{itemlist[tr.Pokemon[i].Item]}");

                if (tr.Pokemon[i].Nature != 0)
                    sb.Append($" (Nature: {natures[tr.Pokemon[i].Nature]})");

                sb.Append($" (Moves: {string.Join("/", tr.Pokemon[i].Moves.Select(m => m == 0 ? "(None)" : movelist[m]))})");
                sb.Append($" IVs: {string.Join("/", tr.Pokemon[i].IVs)}");
                sb.Append($" EVs: {string.Join("/", tr.Pokemon[i].EVs)}");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private void updateNumPokemon(object sender, EventArgs e)
        {
            if (index < 0)
                return;
            Trainers[index].NumPokemon = (int) (NUD_NumPoke.Value);
        }

        private static bool updatingStats;

        private void updateStats(object sender, EventArgs e)
        {
            if (updatingStats)
                return;
            var tb_iv = new[] { TB_HPIV, TB_ATKIV, TB_DEFIV, TB_SPEIV, TB_SPAIV, TB_SPDIV };
            var tb_ev = new[] { TB_HPEV, TB_ATKEV, TB_DEFEV, TB_SPEEV, TB_SPAEV, TB_SPDEV };
            for (int i = 0; i < 6; i++)
            {
                updatingStats = true;
                if (Util.ToInt32(tb_iv[i]) > 31)
                    tb_iv[i].Text = "31";
                if (Util.ToInt32(tb_ev[i]) > 255)
                    tb_ev[i].Text = "255";
                updatingStats = false;
            }

            int species = CB_Pokemon.SelectedIndex;
            species = Main.SpeciesStat[species].FormeIndex(species, CB_Forme.SelectedIndex);
            var p = Main.SpeciesStat[species];
            int level = (int)NUD_Level.Value;
            int Nature = CB_Nature.SelectedIndex;

            ushort[] Stats = new ushort[6];
            Stats[0] =
                (ushort)
                    (p.HP == 1
                        ? 1
                        : (Util.ToInt32(TB_HPIV.Text) + 2 * p.HP + Util.ToInt32(TB_HPEV.Text) / 4 + 100) * level / 100 + 10);
            Stats[1] =
                (ushort)((Util.ToInt32(TB_ATKIV.Text) + 2 * p.ATK + Util.ToInt32(TB_ATKEV.Text) / 4) * level / 100 + 5);
            Stats[2] =
                (ushort)((Util.ToInt32(TB_DEFIV.Text) + 2 * p.DEF + Util.ToInt32(TB_DEFEV.Text) / 4) * level / 100 + 5);
            Stats[4] =
                (ushort)((Util.ToInt32(TB_SPAIV.Text) + 2 * p.SPA + Util.ToInt32(TB_SPAEV.Text) / 4) * level / 100 + 5);
            Stats[5] =
                (ushort)((Util.ToInt32(TB_SPDIV.Text) + 2 * p.SPD + Util.ToInt32(TB_SPDEV.Text) / 4) * level / 100 + 5);
            Stats[3] =
                (ushort)((Util.ToInt32(TB_SPEIV.Text) + 2 * p.SPE + Util.ToInt32(TB_SPEEV.Text) / 4) * level / 100 + 5);

            // Account for nature
            int incr = Nature / 5 + 1;
            int decr = Nature % 5 + 1;
            if (incr != decr)
            {
                Stats[incr] *= 11;
                Stats[incr] /= 10;
                Stats[decr] *= 9;
                Stats[decr] /= 10;
            }

            Stat_HP.Text = Stats[0].ToString();
            Stat_ATK.Text = Stats[1].ToString();
            Stat_DEF.Text = Stats[2].ToString();
            Stat_SPA.Text = Stats[4].ToString();
            Stat_SPD.Text = Stats[5].ToString();
            Stat_SPE.Text = Stats[3].ToString();

            TB_IVTotal.Text = tb_iv.Select(tb => Util.ToInt32(tb)).Sum().ToString();
            TB_EVTotal.Text = tb_ev.Select(tb => Util.ToInt32(tb)).Sum().ToString();

            // Recolor the Stat Labels based on boosted stats.
            {
                incr--;
                decr--;
                Label[] labarray = { Label_ATK, Label_DEF, Label_SPE, Label_SPA, Label_SPD };
                // Reset Label Colors
                foreach (Label label in labarray)
                    label.ResetForeColor();

                // Set Colored StatLabels only if Nature isn't Neutral
                if (incr != decr)
                {
                    labarray[incr].ForeColor = Color.Red;
                    labarray[decr].ForeColor = Color.Blue;
                }
            }
            var ivs = tb_iv.Select(tb => Util.ToInt32(tb) & 1).ToArray();
            updatingStats = true;
            CB_HPType.SelectedIndex = 15 * ((ivs[0]) + 2 * ivs[1] + 4 * ivs[2] + 8 * ivs[3] + 16 * ivs[4] + 32 * ivs[5]) / 63;
            updatingStats = false;
        }

        private void updateHPType(object sender, EventArgs e)
        {
            if (updatingStats)
                return;
            var tb_iv = new[] { TB_HPIV, TB_ATKIV, TB_DEFIV, TB_SPAIV, TB_SPDIV, TB_SPEIV };
            int[] newIVs = setHPIVs(CB_HPType.SelectedIndex, tb_iv.Select(Util.ToInt32).ToArray());
            updatingStats = true;
            TB_HPIV.Text = newIVs[0].ToString();
            TB_ATKIV.Text = newIVs[1].ToString();
            TB_DEFIV.Text = newIVs[2].ToString();
            TB_SPAIV.Text = newIVs[3].ToString();
            TB_SPDIV.Text = newIVs[4].ToString();
            TB_SPEIV.Text = newIVs[5].ToString();
            updatingStats = false;
        }
        public static int[] setHPIVs(int type, int[] ivs)
        {
            for (int i = 0; i < 6; i++)
                ivs[i] = (ivs[i] & 0x1E) + hpivs[type, i];
            return ivs;
        }

        private static readonly int[,] hpivs = {
            { 1, 1, 0, 0, 0, 0 }, // Fighting
            { 0, 0, 0, 0, 0, 1 }, // Flying
            { 1, 1, 0, 0, 0, 1 }, // Poison
            { 1, 1, 1, 0, 0, 1 }, // Ground
            { 1, 1, 0, 1, 0, 0 }, // Rock
            { 1, 0, 0, 1, 0, 1 }, // Bug
            { 1, 0, 1, 1, 0, 1 }, // Ghost
            { 1, 1, 1, 1, 0, 1 }, // Steel
            { 1, 0, 1, 0, 1, 0 }, // Fire
            { 1, 0, 0, 0, 1, 1 }, // Water
            { 1, 0, 1, 0, 1, 1 }, // Grass
            { 1, 1, 1, 0, 1, 1 }, // Electric
            { 1, 0, 1, 1, 1, 0 }, // Psychic
            { 1, 0, 0, 1, 1, 1 }, // Ice
            { 1, 0, 1, 1, 1, 1 }, // Dragon
            { 1, 1, 1, 1, 1, 1 }, // Dark
        };

        private void B_HighAttack_Click(object sender, EventArgs e)
        {

        }
        private void B_CurrentAttack_Click(object sender, EventArgs e)
        {

        }
    }
}
