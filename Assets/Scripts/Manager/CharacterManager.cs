using System;
using System.Collections.Generic;


public class CharacterManager: IManager
{
    private Dictionary<int, Character> characters = new Dictionary<int, Character>();
    private int id;

    public Dictionary<int, Character> Characters { get {
            return characters;
        } 
    }

    public void Init()
    {
        id = 0;
    }

    internal void AddCharacter(Character character)
    {
        characters.Add(id++,character);
    }
}
