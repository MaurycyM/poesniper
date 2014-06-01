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
        
        public enum ItemRarity
        {
            Normal,
            Magic,
            Rare,
            Unique,
        };

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
            BodyArmor,
            Belt,
            Boots,
            Gloves,
            Amulet,
            Ring,
            Shield,
            Quiver,
            OneHandedSword,
            OneHandedAxe,
            OneHandedMace,
            Dagger,
            Wand,
            TwoHandedSword,
            TwoHandedAxe,
            TwoHandedMace,
            Staff,
            Bow,
        }

        public enum ItemBase
        {
            Foo,
            Bar,
            // TODO
        }

        public class Item
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public ItemType Type { get; set; }
            public ItemBase Base { get; set; }
            public ItemRarity Rarity { get; set; }
            public int Quality { get; set; } 
            public Requirements Requirements { get; set; }
            public MagicProperty Implicit { get; set; }
            public ICollection<Socket> Sockets { get; set; }
            public ICollection<MagicProperty> MagicProperties { get; set; }

            public League League { get; set; }
            public ShopThread ShopThread { get; set; }
            public Price Price { get; set; }
            public bool Isverified { get; set; } 
        }

        public class Price
        {
            public Currency Currency { get; set; }
            public int Amount { get; set; }
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

            public int PhysicalDps { get; set; }
            public int ElementalDps { get; set; }
            public int Dps { get; set; }
            public int PhysicalDpsWithMaxQuality { get; set; }
            public int DpsWithMaxQuality { get; set; }
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

        public class Socket
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
    }
}
