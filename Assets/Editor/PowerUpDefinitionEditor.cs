using UnityEditor;
using UnityEngine;

/// <summary>
/// O Inspector de qualquer ficha de poder. Existe para uma coisa só: **mostrar
/// apenas o campo de duração que vale** para a forma escolhida, com o nome que
/// ele tem naquele caso.
///
/// Sem isto, uma ficha de Proteção mostra "Duration Seconds 5" logo abaixo de
/// "Cargas 1", e os dois parecem valer ao mesmo tempo — o campo morto vira
/// convite a mexer no número errado e depois procurar por que não mudou nada.
///
/// Vale para as classes filhas também (<c>editorForChildTypes</c>), e os campos
/// próprios de cada poder continuam aparecendo na ordem em que foram
/// declarados: o desenho é o padrão do Unity, menos o campo escondido.
/// </summary>
[CustomEditor(typeof(PowerUpDefinition), true)]
class PowerUpDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var lifetimeProperty = serializedObject.FindProperty("lifetime");

        // enumValueIndex é a posição no enum, não o valor. Dá na mesma aqui
        // porque Lifetime é 0, 1, 2 na ordem em que foi escrito — se um dia
        // ganhar valor explícito, isto precisa virar enumValueFlag/intValue.
        var lifetime = (PowerUpDefinition.Lifetime)lifetimeProperty.enumValueIndex;

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

    static bool IsHidden(string path, PowerUpDefinition.Lifetime lifetime) =>
        (path == "durationSeconds" && lifetime != PowerUpDefinition.Lifetime.PorTempo) ||
        (path == "charges" && lifetime != PowerUpDefinition.Lifetime.PorUso);

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
