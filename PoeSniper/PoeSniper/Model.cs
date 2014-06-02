using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoeSniper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace PoeSniper.Model
    {
        public enum League
        {
            Standard,
            Hardcore,
            Ambush,
            Invasion,
        }
        
        public enum SocketColor
        {
            Red,
            Green,
            Blue,
            White,
        };

        public enum Currency 
        {
            Alchemy,
            Chaos,
            Exalted,
        }

        public enum ItemType
        {
            Helmet,
            OneHandAxe,
            Flask,
            OneHandSword,
            BodyArmour,
            Map,
            OneHandMace,
            Quiver,
            Amulet,
            FishingRod,
            Sceptre,
            TwoHandAxe,
            Gem,
            TwoHandSword,
            Bow,
            Gloves,
            VaalFragment,
            Claw,
            TwoHandMace,
            Dagger,
            Shield,
            Wand,
            Boots,
            Currency,
            Ring,
            Belt,
            Staff,
        };

        public class Item
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public ItemType Type { get; set; }
            public string Base { get; set; }

            public bool IsVerified { get; set; }
            public bool IsIdentified { get; set; }
            public bool IsCorrupted { get; set; }
            
            public int Quality { get; set; } 
            public Requirements Requirements { get; set; }
            public List<MagicProperty> ImplicitProperties { get; set; }
            public List<MagicProperty> ExplicitProperties { get; set; }
            public List<GemSocket> Sockets { get; set; }

            public League League { get; set; }
            public ShopThread ShopThread { get; set; }
            public Price Price { get; set; }

            public DateTime PostDate { get; set; }
        }

        public class Price
        {
            public Currency Currency { get; set; }
            public decimal Amount { get; set; }
        }

        public class Weapon : Item
        {
            public int MinPhysicalDamage { get; set; }
            public int MaxPhysicalDamage { get; set; }
            public int MinFireDamage { get; set; }
            public int MaxFireDamage { get; set; }
            public int MinColdDamage { get; set; }
            public int MaxColdDamage { get; set; }
            public int MinLightningDamage { get; set; }
            public int MaxLightningDamage { get; set; }
            public int MinChaosDamage { get; set; }
            public int MaxChaosDamage { get; set; }

            public decimal CriticalStrikeChance { get; set; }
            public decimal AttacksPerSecond { get; set; }

            public decimal PhysicalDps { get; set; }
            public decimal ElementalDps { get; set; }
            public decimal Dps { get; set; }
            public decimal PhysicalDpsWithMaxQuality { get; set; }
            public decimal DpsWithMaxQuality { get; set; }
        }

        public class Armor : Item
        {
            public int Armour { get; set; }
            public int Evasion { get; set; }
            public int EnergyShield { get; set; }
        }

        // complex type
        public class Requirements
        {
            public int Level { get; set; }
            public int Strength { get; set; }
            public int Dexterity { get; set; }
            public int Inteligence { get; set; }
        }

        public class MagicProperty
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Value { get; set; }
        }

        public class GemSocket
        {
            public int Id { get; set; }
            public SocketColor Color { get; set; }
            public bool isLinked { get; set; }
        }

        public class Forum
        {
            public int Id { get; set; }
            public string Url { get; set; }
            public DateTime LastShopThreadDate { get; set; }
            public int LastShopThreadPage { get; set; }
            public League League { get; set; }
            public ICollection<ShopThread> ShopThreads { get; set; }
        }

        public class ShopThread
        {
            public int Id { get; set; }
            public string Url { get; set; }
            public string SellerIgn { get; set; }
            public DateTime LastUpdate { get; set; }
            public ICollection<Item> Items { get; set; }
        }

        public class LookingForItem
        {
            public int Id { get; set; }
            public bool? isCorrupted { get; set; }
            public League League { get; set; }
            public LookingForItemType ItemType { get; set; }
            public Price MaxPrice { get; set; }
            public LookingForRequirements MaxRequirements { get; set; }
            public List<LookingForMagicProperty> ImplicitProperties { get; set; }
            public List<LookingForMagicProperty> ExplicitProperties { get; set; }
            public LookingForItemSockets Sockets { get; set; }
        }

        public class LookingForRequirements
        {
            public int? MinLevel { get; set; }
            public int? MaxLevel { get; set; }
            public int? MinStrength { get; set; }
            public int? MaxStrength { get; set; }
            public int? MinDexterity { get; set; }
            public int? MaxDexterity { get; set; }
            public int? MinIntelligence { get; set; }
            public int? MaxIntelligence { get; set; }
        }

        public class LookingForMagicProperty
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int? MinValue { get; set; }
            public int? MaxValue { get; set; }
        }

        public class LookingForItemSockets
        {
            public int? MinNumberOfLinks { get; set; }
            public int? MaxNumberOfLinks { get; set; }
            public int? MinNmberOfRedSockets { get; set; }
            public int? MaxNmberOfRedSockets { get; set; }
            public int? MinNmberOfGreenSockets { get; set; }
            public int? MaxNmberOfGreenSockets { get; set; }
            public int? MinNmberOfBlueSockets { get; set; }
            public int? MaxNmberOfBlueSockets { get; set; }
        }

        public class LookingForWeaponProperties
        {
            public decimal? MinCriticalStrikeChance { get; set; }
            public decimal? MaxCriticalStrikeChance { get; set; }
            public decimal? MinAttacksPerSecond { get; set; }
            public decimal? MaxAttacksPerSecond { get; set; }

            public decimal? MinPhysicalDps { get; set; }
            public decimal? MaxPhysicalDps { get; set; }
            public decimal? MinElementalDps { get; set; }
            public decimal? MaxElementalDps { get; set; }
            public decimal? MinDps { get; set; }
            public decimal? MaxDps { get; set; }
            public decimal? MinPhysicalDpsWithMaxQuality { get; set; }
            public decimal? MaxPhysicalDpsWithMaxQuality { get; set; }
            public decimal? MinDpsWithMaxQuality { get; set; }
            public decimal? MaxDpsWithMaxQuality { get; set; }
        }

        public class LookingForArmorProperties
        {
            public int? MinArmour { get; set; }
            public int? MaxArmour { get; set; }
            public int? MinEvasion { get; set; }
            public int? MaxEvasion { get; set; }
            public int? MinEnergyShield { get; set; }
            public int? MaxEnergyShield { get; set; }
        }

        public enum LookingForItemType
        {
            Helmet,
            OneHandAxe,
            //Flask,
            OneHandSword,
            BodyArmour,
            //Map,
            OneHandMace,
            Quiver,
            Amulet,
            FishingRod,
            Sceptre,
            TwoHandAxe,
            //Gem,
            TwoHandSword,
            Bow,
            Gloves,
            //VaalFragment,
            Claw,
            TwoHandMace,
            Dagger,
            Shield,
            Wand,
            Boots,
            //Currency,
            Ring,
            Belt,
            Staff,
            OneHandWeapon,
            TwoHandWeapon,
        }
    }
}
