using UnityEditor;
using UnityEngine;

/// <summary>
/// O Inspector de qualquer ficha de **modificador de fase**. Existe para uma coisa só:
/// mostrar apenas o campo de duração que vale para a forma escolhida, com o nome
/// que ele tem naquele caso.
///
/// Sem isto, uma ficha de duração por carga mostra "Duration Seconds 5" logo abaixo de
/// "Cargas 1", e os dois parecem valer ao mesmo tempo — o campo morto vira
/// convite a mexer no número errado e depois procurar por que não mudou nada.
///
/// O <see cref="ShipAbilityEditor"/> é o gêmeo disto para poder de nave, e é um
/// arquivo separado de propósito: os dois sistemas não compartilham nada.
/// </summary>
[CustomEditor(typeof(LevelModifier), true)]
class LevelModifierEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // enumValueIndex é a posição no enum, não o valor. Dá na mesma aqui
        // porque Lifetime é 0, 1, 2 na ordem em que foi escrito — se um dia
        // ganhar valor explícito, isto precisa virar intValue.
        var lifetime = (LevelModifier.Lifetime)
            serializedObject.FindProperty("lifetime").enumValueIndex;

        var property = serializedObject.GetIterator();
        bool enterChildren = true;

        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (property.propertyPath == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(property);

                continue;
            }

            if (IsHidden(property.propertyPath, lifetime))
                continue;

            EditorGUILayout.PropertyField(property, LabelFor(property), true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    static bool IsHidden(string path, LevelModifier.Lifetime lifetime)
    {
        switch (path)
        {
            case "durationSeconds":
                return lifetime != LevelModifier.Lifetime.PorTempo;

            case "charges":
                return lifetime != LevelModifier.Lifetime.PorUso;

            default:
                return false;
        }
    }

    /// <summary>
    /// O rótulo em português para os campos cujo nome em inglês não se explica
    /// sozinho. O resto fica com o que o Unity gera do nome do campo.
    /// </summary>
    static GUIContent LabelFor(SerializedProperty property)
    {
        switch (property.propertyPath)
        {
            case "charges":
                return new GUIContent("Cargas", property.tooltip);
            case "durationSeconds":
                return new GUIContent("Duração (s)", property.tooltip);
            default:
                return new GUIContent(property.displayName, property.tooltip);
        }
    }
}
