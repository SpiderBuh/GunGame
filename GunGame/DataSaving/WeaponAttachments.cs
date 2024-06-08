using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using static GunGame.GunGameUtils;

namespace GunGame.DataSaving
{
    public class WeaponAttachments
    {
        public const string FilePath = "GunGameWeaponData.xml";

        [Serializable]
        public class WeaponDataWrapper
        {
            public WeaponDataWrapper() { }
            public WeaponDataWrapper(List<ggWeapon> weapons)
            {
                Weapons = weapons;
            }
            public List<ggWeapon> Weapons { get; set; } = new List<ggWeapon>();
            public Gat GetRandomGat(bool weighted = true, bool stack = true)
            {
                Random rnd = new Random();
                // Calculate the total weight (sum of all rankings)
                float totalWeight = 0;
                foreach (var item in Weapons)
                {
                    if (weighted)
                        totalWeight += item.TempRanking;
                    else
                        totalWeight += item.AverageRanking();
                }

                // Generate a random number between 0 and totalWeight
                float randomNumber = Convert.ToSingle(rnd.NextDouble() * totalWeight);

                // Select an element based on the random number
                //float cumulativeWeight = 0;
                foreach (var item in Weapons)
                {
                    if (weighted)
                        randomNumber -= item.TempRanking;
                    //cumulativeWeight += item.TempRanking;
                    else
                        randomNumber -= item.AverageRanking();
                    //cumulativeWeight += item.AverageRanking();
                    if (randomNumber < 0)
                    {
                        if (stack)
                            item.TimesChosen++;
                            //item.TempRanking = Math.Max(0.01f, item.TempRanking - 25);
                        return new Gat(item.Item, item.GetRandomAttachments(0, true, weighted, stack));
                    }
                }

                // Fallback
                return new Gat(ItemType.KeycardJanitor, Convert.ToUInt32(randomNumber));
            }
            public void UpdateRankings(List<Gat> gats, float averageGameKD)
            {
                foreach (var gat in gats)
                    for (int i = 0; i < AllWeapons.Count; i++)
                        if (Weapons[i].Item == gat.ItemType)
                        {
                            Weapons[i].UpdateRanking((gat.kills / gat.deaths) / gat.usedBy, averageGameKD, gat.Mod);
                            break;
                        }
            }
        }

        [Serializable]
        public class ggWeapon
        {
            public ItemType Item { get; set; }
            [XmlIgnore]
            public ItemBase ItemBaseRef
            {
                get
                {
                    InventoryItemLoader.TryGetItem(Item, out ItemBase result);
                    return result;
                }
            }
            [XmlIgnore]
            public float TempRanking => Slots.Average(x => x.AverageRanking()) - 25 * TimesChosen;
            [XmlIgnore]
            public uint TimesChosen { get; set; } = 0;
            public List<ggAttachmentSlot> Slots { get; set; } = new List<ggAttachmentSlot>();
            public ggWeapon() { }
            public ggWeapon(ItemType item)
            {
                this.Item = item;
                Slots = new List<ggAttachmentSlot>();
                if (InventoryItemLoader.TryGetItem(item, out ItemBase found) && found is Firearm firearm)
                {
                    List<ggAttachment> attachments = new List<ggAttachment>();
                    var prevAtt = firearm.Attachments.FirstOrDefault();
                    foreach (var att in firearm.Attachments)
                    {
                        if (att.Slot == prevAtt.Slot)
                            attachments.Add(new ggAttachment(att.Name));
                        else
                        {
                            Slots.Add(new ggAttachmentSlot(prevAtt.Slot, attachments));
                            attachments.Clear();
                            attachments.Add(new ggAttachment(att.Name));
                        }
                        prevAtt = att;
                    }
                    Slots.Add(new ggAttachmentSlot(prevAtt.Slot, attachments));
                }
                else
                    Slots = new List<ggAttachmentSlot>() { new ggAttachmentSlot(AttachmentSlot.Unassigned, new List<ggAttachment>() { new ggAttachment(AttachmentName.None) }) };
                //TempRanking = Slots.Average(x => x.AverageRanking());
            }
            /// <param name="nonrandomAttachments">An attachment code with all 0's for slots that will be randomised. Leave as 0 to randomize everything.</param>
            /// <param name="validate">Call the AttachmentsUtils.ValidateAttachmentsCode method before returning.</param>
            /// <returns>A random attachment code</returns>
            public uint GetRandomAttachments(uint nonrandomAttachments = 0, bool validate = true, bool weighted = true, bool stack = true)
            {
                uint code = nonrandomAttachments;
                int pointerPower = 0;
                foreach (var slot in Slots)
                { //bitwise stuff
                    if ((code & (slot.GetMask << pointerPower)) == 0)
                        code += slot.GetRandomAttachment(weighted, stack) << pointerPower;
                    pointerPower += slot.NumAttachments;
                }
                if (validate && ItemBaseRef is Firearm firearmRef)
                    return AttachmentsUtils.ValidateAttachmentsCode(firearmRef, code);
                else
                    return code;
            }
            private void UpdateRankingAtIndex(float kdPerCapita, float averageGameKD, int index)
            {
                int offset = 0;
                foreach (var slot in Slots)
                    if (index - slot.NumAttachments >= 0)
                    {
                        index -= slot.NumAttachments;
                        offset++;
                    }
                    else
                        Slots[offset].SlotAttachments[index].UpdateRanking(kdPerCapita, averageGameKD);
            }
            public void UpdateRanking(float kdPerCapita, float averageGameKD, uint attachmentsCode)
            {
                int index = 0, truncCode = (int)attachmentsCode;
                while (truncCode > 0)
                {
                    if ((truncCode & 1) > 0)
                        UpdateRankingAtIndex(kdPerCapita, averageGameKD, index);
                    index++;
                    truncCode >>= 1;
                }
            }
            public float AverageRanking(bool weighted = false)
            {
                return Slots.Average(x => x.AverageRanking(weighted));
            }
            public override string ToString()
            {
                return Item.ToString();
            }
        }
        [Serializable]
        public class ggAttachmentSlot
        {
            public AttachmentSlot AttachmentSlot { get; set; }
            public List<ggAttachment> SlotAttachments { get; set; }
            public int NumAttachments => SlotAttachments.Count;
            public uint GetMask
            {
                get
                {
                    uint mask = 0;
                    for (int i = 0; i < NumAttachments; i++)
                        mask += (uint)1 << i;
                    return mask;
                }
            }

            public ggAttachmentSlot()
            {
                AttachmentSlot = AttachmentSlot.Unassigned;
                SlotAttachments = new List<ggAttachment>() { new ggAttachment() };
            }
            public ggAttachmentSlot(AttachmentSlot attachmentSlot, List<ggAttachment> slotAttachments)
            {
                AttachmentSlot = attachmentSlot;
                SlotAttachments = slotAttachments;
            }
            public uint GetRandomAttachment(bool weighted = true, bool stack = true)
            {
                if (weighted)
                    return GetRandomWeightedAttachment(stack);
                Random rnd = new Random();
                return (uint)(1 << rnd.Next(0, NumAttachments - 1));
            }
            public uint GetRandomWeightedAttachment(bool stack = true)
            {
                Random rnd = new Random();
                // Calculate the total weight (sum of all rankings)
                float totalWeight = 0;
                foreach (var item in SlotAttachments)
                {
                    if (stack)
                        totalWeight += item.TempRanking;
                    else
                        totalWeight += item.Ranking;
                }

                // Generate a random number between 0 and totalWeight
                float randomNumber = (float)(rnd.NextDouble() * totalWeight);

                // Select an element based on the random number
                //float cumulativeWeight = 0;
                for (int i = 0; i < SlotAttachments.Count; i++)
                {
                    ggAttachment item = SlotAttachments[i];
                    if (stack)
                        randomNumber -= item.TempRanking;
                    else
                        randomNumber -= item.Ranking;
                    if (randomNumber < 0)
                    {
                        if (stack)
                            SlotAttachments[i].TimesChosen++;
                        //item.TempRanking = Math.Max(0.01f, item.TempRanking - 25);
                        return (uint)1 << i;
                    }
                }

                //Fallback
                return 1;
            }

            public void UpdateRanking(float kdPerCapita, float averageGameKD, AttachmentName attachment)
            {
                var match = SlotAttachments.FindIndex(x => x.AttachmentName == attachment);
                if (match != -1)
                    SlotAttachments[match].UpdateRanking(kdPerCapita, averageGameKD);
            }
            internal float AverageRanking(bool weighted = false) //Internal due to the average ranking of a slot without the context of every other slot doesn't really make sense
            {
                if (weighted)
                    return SlotAttachments.Average(x => x.TempRanking);
                return SlotAttachments.Average(x => x.Ranking);
            }
            public override string ToString()
            {
                return AttachmentSlot.ToString();
            }
        }
        [Serializable]
        public class ggAttachment
        {
            public AttachmentName AttachmentName { get; set; }
            public float Ranking { get; set; }
            [XmlIgnore]
            public float TempRanking => Ranking - 25 * TimesChosen;
            [XmlIgnore]
            public uint TimesChosen { get; set; } = 0;
            public void UpdateRanking(float kdPerCapita, float averageGameKD)
            {
                Ranking += ((kdPerCapita - averageGameKD) / averageGameKD) / Math.Max(Math.Abs(Ranking - 50f), 0.5f); //Equation that adds or removes a tiny portion of the ranking based on how extreme the ranking is already
                Ranking = Math.Max(0.01f, Math.Min(Ranking, 100f));
            }
            public ggAttachment() { AttachmentName = AttachmentName.None; Ranking = 50f; }
            public ggAttachment(AttachmentName attachmentName = AttachmentName.None, float ranking = 50f)
            {
                AttachmentName = attachmentName;
                Ranking = Math.Max(0.01f, Math.Min(ranking, 100f));
            }
            public override string ToString()
            {
                return AttachmentName.ToString();
            }
        }
    }
}
