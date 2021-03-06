﻿using System.Collections.Generic;
using System.Linq;

using pk3DS.Core.Structures.PersonalInfo;

namespace pk3DS.Core.Randomizers
{
    public class SpeciesRandomizer
    {
        private readonly GameConfig Game;
        private readonly PersonalInfo[] SpeciesStat;
        private readonly int MaxSpeciesID;
        public int[] speciesList { get; private set; }

        public SpeciesRandomizer(GameConfig config)
        {
            Game = config;
            MaxSpeciesID = Game.Info.MaxSpeciesID;
            SpeciesStat = Game.Personal.Table;
        }

        /// <summary>
        /// To be called after the allowed species are provided.
        /// </summary>
        public void Initialize()
        {
            speciesList = InitializeSpeciesList();
            RandSpec = new GenericRandomizer(speciesList);
        }

        #region Randomizer Settings
        public bool G1 = true;
        public bool G2 = true;
        public bool G3 = true;
        public bool G4 = true;
        public bool G5 = true;
        public bool G6 = true;
        public bool G7 = false;
        public bool L = false;
        public bool E = false;
        public bool Shedinja = false;
        public bool rEXP = false;
        public bool rBST = true;
        public bool rType = false;
        #endregion

        #region Random Species Filtering Parameters
        private GenericRandomizer RandSpec;
        private int loopctr;
        public int l = 10; // tweakable scalars
        public int h = 11;
		private List<int> legendary = new List<int>();
        #endregion

        internal int GetRandomSpecies(int oldSpecies, int bannedSpecies)
        {
            // Get a new random species
            PersonalInfo oldpkm = SpeciesStat[oldSpecies];

            loopctr = 0; // altering calculations to prevent infinite loops
            int newSpecies;
            while (!GetNewSpecies(bannedSpecies, oldpkm, out newSpecies))
                loopctr++;
            return newSpecies;
        }

        public int GetRandomSpeciesType(int oldSpecies, int type)
        {
            // Get a new random species
            PersonalInfo oldpkm = SpeciesStat[oldSpecies];

            loopctr = 0; // altering calculations to prevent infinite loops
            int newSpecies;
            while (!GetNewSpecies(oldSpecies, oldpkm, out newSpecies) || !GetIsTypeMatch(newSpecies, type))
                loopctr++;
            return newSpecies;
        }

        private bool GetIsTypeMatch(int newSpecies, int type) => type == -1 || SpeciesStat[newSpecies].Types.Any(z => z == type) || loopctr > 9000;

        public int GetRandomSpecies(int oldSpecies)
        {
            // Get a new random species
            PersonalInfo oldpkm = SpeciesStat[oldSpecies];

            loopctr = 0; // altering calculations to prevent infinite loops
            int newSpecies;
            while (!GetNewSpecies(oldSpecies, oldpkm, out newSpecies))
            {
                if (loopctr > 0x0001_0000)
                {
                    PersonalInfo pkm = SpeciesStat[newSpecies];
                    if (IsSpeciesBSTBad(oldpkm, pkm) && loopctr > 0x0001_1000) // keep trying for at minimum BST
                        continue;
                    return newSpecies; // failed to find any match based on criteria, return random species that may or may not match criteria
                }
                loopctr++;
            }
            return newSpecies;
        }

        public int GetRandomSpeciesNotSelected(int oldSpecies, List<int> selected)
        {
	        PersonalInfo oldpkmn = SpeciesStat[oldSpecies];

	        loopctr = 0;
	        int newSpecies;
	        bool except = !speciesList.Except(selected).Except(legendary).Any();
	        while (!GetNewSpecies(oldSpecies, oldpkmn, out newSpecies) 
	               || !except && selected.Contains(newSpecies))
			{
				if (legendary.Contains(newSpecies) && !legendary.Contains(oldSpecies) && loopctr < 0x10)
					continue;

				if (loopctr > 0x0001_0000)
		        {
			        PersonalInfo pkm = SpeciesStat[newSpecies];
			        if (IsSpeciesBSTBad(oldpkmn, pkm) && loopctr > 0x0001_1000)
				        continue;
			        return newSpecies;
		        }

		        loopctr++;
	        }

	        return newSpecies;
        }

        private bool IsSpeciesReplacementBad(int newSpecies, int currentSpecies)
        {
            return newSpecies == currentSpecies && loopctr < MaxSpeciesID * 10;
        }

        private bool IsSpeciesEXPRateBad(PersonalInfo oldpkm, PersonalInfo pkm)
        {
            if (!rEXP)
                return false;
            // Experience Growth Rate matches
            return oldpkm.EXPGrowth != pkm.EXPGrowth;
        }

        private bool IsSpeciesTypeBad(PersonalInfo oldpkm, PersonalInfo pkm)
        {
            if (!rType)
                return false;
            // Type has to be somewhat similar
            return !oldpkm.Types.Any(z => pkm.Types.Contains(z));
        }

        private bool IsSpeciesBSTBad(PersonalInfo oldpkm, PersonalInfo pkm)
        {
            if (!rBST)
                return false;
            // Base stat total has to be close to original BST
            int expand = loopctr / MaxSpeciesID;
            int lo = oldpkm.BST * l / (h + expand);
            int hi = oldpkm.BST * (h + expand) / l;
            return lo > pkm.BST || pkm.BST > hi;
        }

        private int[] InitializeSpeciesList()
        {
            List<int> list = new List<int>();
            if (G1) AddGen1Species(list);
            if (G2) AddGen2Species(list);
            if (G3) AddGen3Species(list);
            if (G4) AddGen4Species(list);
            if (G5) AddGen5Species(list);
            if (G6) AddGen6Species(list);
            if (G7) AddGen7Species(list);

            return list.Count == 0 ? RandomSpeciesList : list.ToArray();
        }

        private void AddGen1Species(List<int> list)
        {
            list.AddRange(Enumerable.Range(1, 143)); // Bulbasaur - Snorlax
            list.AddRange(Enumerable.Range(147, 3)); // Dratini - Dragonite

            if (L)
            {
                list.AddRange(Enumerable.Range(144, 3)); // Birds
                list.Add(150); // Mewtwo
				legendary.AddRange(Enumerable.Range(144, 3));
				legendary.Add(150);
            }
            if (E)
            {
	            list.Add(151); // Mew
				legendary.Add(151);
			}
        }

        private void AddGen2Species(List<int> list)
        {
            list.AddRange(Enumerable.Range(152, 91)); // Chikorita - Blissey
            list.AddRange(Enumerable.Range(246, 3)); // Larvitar - Tyranitar

            if (L)
            {
                list.AddRange(Enumerable.Range(243, 3)); // Raikou, Entei, Suicune
                list.AddRange(Enumerable.Range(249, 2)); // Lugia & Ho-Oh
                legendary.AddRange(Enumerable.Range(243, 3));
                legendary.AddRange(Enumerable.Range(249, 2));
			}

            if (E)
            {
	            list.Add(251); // Celebi
				legendary.Add(251);
            }
        }

        private void AddGen3Species(List<int> list)
        {
            list.AddRange(Enumerable.Range(252, 40)); // Treecko - Ninjask
            list.AddRange(Enumerable.Range(293, 84)); // Whismur - Metagross
            if (Shedinja) list.Add(292); // Shedinja
            if (L)
            {
	            list.AddRange(Enumerable.Range(377, 8)); // Regi, Lati, Mascot
	            legendary.AddRange(Enumerable.Range(377, 8));
			}
            if (E)
            {
	            list.AddRange(Enumerable.Range(385, 2)); // Jirachi/Deoxys
	            legendary.AddRange(Enumerable.Range(385, 2));
			}
        }

        private void AddGen4Species(List<int> list)
        {
            list.AddRange(Enumerable.Range(387, 93)); // Turtwig - Rotom
            if (L)
            {
	            list.AddRange(Enumerable.Range(480, 9)); // Sinnoh Legends
				legendary.AddRange(Enumerable.Range(480, 9));
            }

            if (E)
            {
	            list.AddRange(Enumerable.Range(489, 5)); // Phione, Manaphy, Darkrai, Shaymin, Arceus
				legendary.AddRange(Enumerable.Range(489, 5));
            }
        }

        private void AddGen5Species(List<int> list)
        {
            list.AddRange(Enumerable.Range(495, 143)); // Snivy - Volcarona
            if (L)
            {
	            list.AddRange(Enumerable.Range(638, 9)); // Unova Legends
	            legendary.AddRange(Enumerable.Range(638, 9));
			}

            if (E)
            {
	            list.Add(494);
	            list.AddRange(Enumerable.Range(647, 3)); // Victini, Keldeo, Meloetta, Genesect
	            legendary.Add(494);
	            legendary.AddRange(Enumerable.Range(647, 3));
			}
        }

        private void AddGen6Species(List<int> list)
        {
            list.AddRange(Enumerable.Range(650, 66)); // Chespin - Noivern
            if (L)
            {
	            list.AddRange(Enumerable.Range(716, 3)); // Kalos Legends
	            legendary.AddRange(Enumerable.Range(716, 3));
			}

            if (E)
            {
	            list.AddRange(Enumerable.Range(719, 3)); // Diancie, Hoopa, Volcanion
	            legendary.AddRange(Enumerable.Range(719, 3));
			}
        }

        private void AddGen7Species(List<int> list)
        {
            list.AddRange(Enumerable.Range(722, 50)); // Rowlet - Pyukumuku
            list.AddRange(Enumerable.Range(774, 11)); // Minior - Kommo-o

            if (L)
            {
                list.AddRange(Enumerable.Range(772, 2)); // Type: Null, Silvally
                list.AddRange(Enumerable.Range(785, 16)); // Tapus, Legends, UBs
                legendary.AddRange(Enumerable.Range(772, 2));
                legendary.AddRange(Enumerable.Range(785, 16));
			}
            if (E)
            {
	            list.AddRange(Enumerable.Range(801, 2)); // Magearna, Marshadow
	            legendary.AddRange(Enumerable.Range(801, 2));
            }

			if (MaxSpeciesID == 807) // USUM
            {
                if (L)
                {
	                list.AddRange(Enumerable.Range(803, 4)); // Poipole, Naganadel, Stakataka, Blacephalon
	                legendary.AddRange(Enumerable.Range(803, 4));
                }

				if (E)
                {
	                list.Add(807); // Zeraora
					legendary.Add(807);
                }
            }
        }

        public int[] RandomSpeciesList => Enumerable.Range(1, MaxSpeciesID).ToArray();

        private bool IsSpeciesGood(int currentSpecies, PersonalInfo oldpkm, int newSpecies, PersonalInfo pkm)
        {
	        if (IsSpeciesReplacementBad(newSpecies, currentSpecies)) // no A->A randomization
		        return false;
	        if (IsSpeciesEXPRateBad(oldpkm, pkm))
		        return false;
	        if (IsSpeciesTypeBad(oldpkm, pkm))
		        return false;
	        if (IsSpeciesBSTBad(oldpkm, pkm))
		        return false;
	        return true;
        }

        private bool GetNewSpecies(int currentSpecies, PersonalInfo oldpkm, out int newSpecies)
        {
            newSpecies = RandSpec.Next();
            PersonalInfo pkm = SpeciesStat[newSpecies];

            // Verify it meets specifications
			return IsSpeciesGood(currentSpecies, oldpkm, newSpecies, pkm);
        }
    }
}
