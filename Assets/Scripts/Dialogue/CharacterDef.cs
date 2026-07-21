using System.Collections.Generic;
using UnityEngine;

public enum PortraitSide
{
    Left,
    Right
}

// Definición de un personaje: su nombre, su lado por defecto en pantalla y su PROPIO conjunto
// de expresiones (no hay lista global). Es un ScriptableObject porque referencia sprites.
[CreateAssetMenu(fileName = "NewCharacter", menuName = "McClarens/Character")]
public class CharacterDef : ScriptableObject
{
    [System.Serializable]
    public class Expression
    {
        public string id;
        public Sprite portrait;
    }

    [Tooltip("Nombre exacto que se usará en los archivos .mcc (ej: Camarero).")]
    public string characterName;

    [Tooltip("Lado por defecto del retrato. NPC suele ser Left, el jugador Right.")]
    public PortraitSide defaultSide = PortraitSide.Left;

    public List<Expression> expressions = new List<Expression>();

    public bool HasExpression(string id)
    {
        foreach (var e in expressions)
        {
            if (e.id == id)
            {
                return true;
            }
        }

        return false;
    }

    public Sprite GetPortrait(string id)
    {
        foreach (var e in expressions)
        {
            if (e.id == id)
            {
                return e.portrait;
            }
        }

        return null;
    }

    public string DefaultExpressionId => expressions.Count > 0 ? expressions[0].id : null;
}
