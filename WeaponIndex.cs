using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nation_Internal
{
    internal static class WeaponIndex
    {
        internal static string GetWeaponName(int index)
        {
            return index switch
            {
                0 => "AK47",
                1 => "FIST",
                2 => "M4A1",
                3 => "USP",
                4 => "AWM",
                5 => "M1014",
                6 => "AK47",
                7 => "UMP",
                8 => "MP5",
                9 => "Desert-Eagle",
                10 => "G18",
                11 => "M14",
                12 => "SCAR",
                13 => "VSS",
                14 => "GROZA",
                15 => "MP40",
                16 => "PAN",
                17 => "PARANG",
                18 => "SKS",
                19 => "M249",
                20 => "M1873",
                21 => "KAR98K",
                24 => "FAMAS",
                25 => "M500",
                26 => "SVD",
                27 => "BAT",
                28 => "XM8",
                29 => "SPAS12",
                30 => "M60",
                32 => "P90",
                33 => "AN94",
                34 => "KATANA",
                35 => "CG15",
                39 => "PLASMA",
                41 => "M1887",
                43 => "THOMPSON",
                45 => "M82B",
                46 => "AUG",
                47 => "PARAFAL",
                48 => "WOODPECKER",
                49 => "VECTOR",
                50 => "MAG7",
                51 => "SCYTHE",
                54 => "KORD",
                55 => "M1917",
                56 => "USP2",
                57 => "KINGFISHER",
                58 => "MINI-UZI",
                60 => "MP5",//-I
                61 => "M60",//-I
                62 => "VSS",//-I
                63 => "M14",//-I
                64 => "KAR98K",//-I
                65 => "AWM-Y",
                67 => "FAMAS-I",
                70 => "GROZA", //-X
                71 => "M249",//-X
                72 => "SVD",//-Y
                73 => "G36", //G36-ASSAULT
                74 => "G36", //G36-RANGE
                75 => "M24",
                78 => "HEALSNIPER",
                80 => "M4A1",//-I
                81 => "M4A1",//-II
                82 => "M4A1",//-III
                86 => "CHARGE BUSTER",
                88 => "MAC10",
                89 => "AC80",
                93 => "HEAL-PISTOL",
                99 => "SHIELD-GUN",
                100 => "FLAMTHROWER",
                119 => "M1887",//-X
                120 => "MP5",//-II
                121 => "MP5",//-III
                122 => "M60",//-II
                123 => "M60",//-III
                124 => "VSS",//-II
                125 => "VSS",//-III
                126 => "M14",//-II
                127 => "M14",//-III
                128 => "KAR98K",//-II
                129 => "KAR98K",//-III
                130 => "FAMAS",//-II
                131 => "FAMAS",//-III
                181 => "TROGON",
                150 => "BIZON",
                21002 => "M590",
                197 => "VSK94",
                178 => "SCAR",//-I
                179 => "SCAR",//-II
                180 => "SCAR",//-III
                193 => "AUG",//-I
                194 => "AUG",//-II
                195 => "AUG",//-III
                228 => "MAC10",//-I
                229 => "MAC10",//-II
                230 => "MAC10",//-III
                184 => "M1014",//-I
                185 => "M1014",//-II
                186 => "M1014",//-III
                21001 => "HEAL-PISTOL",//-Y
                _ => $"Unknown Weapon [{index}]"
            };
        }
    }
}
