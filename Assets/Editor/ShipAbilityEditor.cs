using UnityEditor;
using UnityEngine;

/// <summary>
/// O Inspector de qualquer ficha de **poder da nave**. Mesmo papel do
/// <see cref="LevelPowerUpEditor"/>, e arquivo separado de propósito: os dois
/// sistemas de poder não compartilham nada, nem aqui.
///
/// Além de esconder o campo de duração que não vale, ele nomeia em português os
/// dois limites que mais se confundem — **usos por partida** (quantas vezes o
/// jogador liga) e **cargas** (quanto o efeito aguenta depois de ligado).
/// </summary>
[CustomEditor(typeof(ShipAbility), true)]
class ShipAbilityEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var lifetime = (ShipAbility.Lifetime)
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

    static bool IsHidden(string path, ShipAbility.Lifetime lifetime)
    {
        switch (path)
        {
            case "durationSeconds":
                return lifetime != ShipAbility.Lifetime.PorTempo;

            case "charges":
                return lifetime != ShipAbility.Lifetime.PorUso;

            default:
                return false;
        }
    }

    static GUIContent LabelFor(SerializedProperty property)
    {
        switch (property.propertyPath)
        {
            case "usesPerRace":
                return new GUIContent("Usos por partida", property.tooltip);
            case "cooldownSeconds":
                return new GUIContent("Recarga (s)", property.tooltip);
            case "charges":
                return new GUIContent("Cargas do efeito", property.tooltip);
            case "durationSeconds":
                return new GUIContent("Duração (s)", property.tooltip);
            default:
                return new GUIContent(property.displayName, property.tooltip);
        }
    }
}
