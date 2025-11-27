using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Grid;
using Enemy;

/// <summary>
/// Custom inspector for HordesManager that constrains spawn node selection to grid spawn cells.
/// </summary>
[CustomEditor(typeof(HordesManager))]
public class HordesManagerEditor : Editor
{
    #region Variables And Properties
    #region Serialized
    private SerializedProperty gridProperty;
    private SerializedProperty gameManagerProperty;
    private SerializedProperty hordesProperty;
    private SerializedProperty defenceStartDelayProperty;
    #endregion

    #region Styles
    private GUIStyle headerStyle;
    private GUIStyle foldoutStyle;
    private GUIStyle boxStyle;
    #endregion
    #endregion

    #region Methods
    #region Unity
    /// <summary>
    /// Caches serialized properties and initializes GUI styles.
    /// </summary>
    private void OnEnable()
    {
        gridProperty = serializedObject.FindProperty("grid");
        gameManagerProperty = serializedObject.FindProperty("gameManager");
        hordesProperty = serializedObject.FindProperty("hordes");
        defenceStartDelayProperty = serializedObject.FindProperty("defenceStartDelay");
        BuildStyles();
    }

    /// <summary>
    /// Renders a structured inspector with grouped sections.
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawCoreSection();
        DrawHordesSection();

        serializedObject.ApplyModifiedProperties();
    }
    #endregion

    #region Drawing
    /// <summary>
    /// Renders core reference fields in a compact box.
    /// </summary>
    private void DrawCoreSection()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        DrawSectionHeader("Core References");
        EditorGUILayout.PropertyField(gridProperty, new GUIContent("Grid"));
        EditorGUILayout.PropertyField(gameManagerProperty, new GUIContent("Game Manager"));
        EditorGUILayout.PropertyField(defenceStartDelayProperty, new GUIContent("Defence Start Delay"));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    /// <summary>
    /// Renders the hordes list with wave and spawn configuration.
    /// </summary>
    private void DrawHordesSection()
    {
        hordesProperty.isExpanded = EditorGUILayout.Foldout(hordesProperty.isExpanded, "Hordes", true, foldoutStyle);
        if (!hordesProperty.isExpanded)
        {
            return;
        }

        Grid3D gridTarget = gridProperty.objectReferenceValue as Grid3D;
        Vector2Int[] spawnCoords = gridTarget != null ? gridTarget.GetEnemySpawnCoords() : System.Array.Empty<Vector2Int>();
        string[] spawnLabels = BuildSpawnLabels(spawnCoords);

        EditorGUI.indentLevel++;
        int hordeCount = hordesProperty.arraySize;
        for (int i = 0; i < hordeCount; i++)
        {
            SerializedProperty hordeProperty = hordesProperty.GetArrayElementAtIndex(i);
            SerializedProperty keyProperty = hordeProperty.FindPropertyRelative("key");
            SerializedProperty wavesProperty = hordeProperty.FindPropertyRelative("waves");

            EditorGUILayout.BeginVertical(boxStyle);
            DrawSectionHeader($"Horde {i + 1}");
            EditorGUILayout.PropertyField(keyProperty, new GUIContent("Key"));
            wavesProperty.isExpanded = EditorGUILayout.Foldout(wavesProperty.isExpanded, "Waves", true, foldoutStyle);
            if (wavesProperty.isExpanded)
            {
                EditorGUI.indentLevel++;
                DrawWavesList(wavesProperty, spawnCoords, spawnLabels);
                DrawWaveArrayControls(wavesProperty, spawnCoords);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// Renders each wave entry with enemy composition and spawn settings.
    /// </summary>
    private void DrawWavesList(SerializedProperty wavesProperty, Vector2Int[] spawnCoords, string[] spawnLabels)
    {
        int waveCount = wavesProperty.arraySize;
        for (int i = 0; i < waveCount; i++)
        {
            SerializedProperty waveProperty = wavesProperty.GetArrayElementAtIndex(i);
            SerializedProperty enemyTypesProperty = waveProperty.FindPropertyRelative("enemyTypes");
            SerializedProperty spawnAssignmentsProperty = waveProperty.FindPropertyRelative("spawnAssignments");
            SerializedProperty spawnNodesProperty = waveProperty.FindPropertyRelative("spawnNodes");
            SerializedProperty spawnCadenceProperty = waveProperty.FindPropertyRelative("spawnCadenceSeconds");
            SerializedProperty advanceModeProperty = waveProperty.FindPropertyRelative("advanceMode");
            SerializedProperty advanceDelayProperty = waveProperty.FindPropertyRelative("advanceDelaySeconds");

            waveProperty.isExpanded = EditorGUILayout.Foldout(waveProperty.isExpanded, $"Wave {i + 1}", true, foldoutStyle);
            if (waveProperty.isExpanded)
            {
                EditorGUI.indentLevel++;
                DrawEnemyTypesList(enemyTypesProperty);
                EditorGUILayout.PropertyField(spawnCadenceProperty);
                DrawSpawnAssignments(spawnAssignmentsProperty, enemyTypesProperty, spawnCoords, spawnLabels);
                DrawSpawnNodesSelector(spawnNodesProperty, spawnCoords, spawnLabels);
                EditorGUILayout.PropertyField(advanceModeProperty);
                EditorGUILayout.PropertyField(advanceDelayProperty);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
        }
    }

    /// <summary>
    /// Renders enemy type definitions in boxed rows with remove and add controls.
    /// </summary>
    private void DrawEnemyTypesList(SerializedProperty enemyTypesProperty)
    {
        if (enemyTypesProperty == null)
            return;

        enemyTypesProperty.isExpanded = EditorGUILayout.Foldout(enemyTypesProperty.isExpanded, "Enemy Types", true, foldoutStyle);
        if (!enemyTypesProperty.isExpanded)
            return;

        EditorGUI.indentLevel++;
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(12f);
        EditorGUILayout.BeginVertical();
        int typeCount = enemyTypesProperty.arraySize;
        for (int i = 0; i < typeCount; i++)
        {
            SerializedProperty typeProperty = enemyTypesProperty.GetArrayElementAtIndex(i);
            SerializedProperty labelProperty = typeProperty.FindPropertyRelative("label");
            SerializedProperty definitionProperty = typeProperty.FindPropertyRelative("enemyDefinition");
            SerializedProperty modifiersProperty = typeProperty.FindPropertyRelative("runtimeModifiers");
            SerializedProperty countProperty = typeProperty.FindPropertyRelative("enemyCount");
            SerializedProperty offsetProperty = typeProperty.FindPropertyRelative("spawnOffset");

            EditorGUILayout.BeginVertical("box");
            SerializedProperty foldoutProperty = typeProperty.FindPropertyRelative("isExpanded");
            bool currentExpanded = foldoutProperty != null ? foldoutProperty.boolValue : true;
            bool newExpanded = EditorGUILayout.Foldout(currentExpanded, $"Enemy Type {i + 1}", true, foldoutStyle);
            if (foldoutProperty != null)
                foldoutProperty.boolValue = newExpanded;

            if (newExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(labelProperty);
                EditorGUILayout.PropertyField(definitionProperty, new GUIContent("Definition"));
                EditorGUILayout.PropertyField(modifiersProperty, new GUIContent("Runtime Modifiers"), true);
                EditorGUILayout.PropertyField(countProperty, new GUIContent("Enemy Count"));
                EditorGUILayout.PropertyField(offsetProperty, new GUIContent("Spawn Offset"));
                EditorGUI.indentLevel--;
            }

            if (GUILayout.Button("Remove Enemy Type"))
            {
                enemyTypesProperty.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
                return;
            }
            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add Enemy Type"))
        {
            int newIndex = enemyTypesProperty.arraySize;
            enemyTypesProperty.InsertArrayElementAtIndex(newIndex);
            SerializedProperty newType = enemyTypesProperty.GetArrayElementAtIndex(newIndex);
            if (newType != null)
            {
                SerializedProperty labelProp = newType.FindPropertyRelative("label");
                if (labelProp != null)
                    labelProp.stringValue = string.Empty;

                SerializedProperty definitionProp = newType.FindPropertyRelative("enemyDefinition");
                if (definitionProp != null)
                    definitionProp.objectReferenceValue = null;

                SerializedProperty modifiersProp = newType.FindPropertyRelative("runtimeModifiers");
                if (modifiersProp != null)
                    modifiersProp.boxedValue = default(EnemyRuntimeModifiers);

                SerializedProperty countProp = newType.FindPropertyRelative("enemyCount");
                if (countProp != null)
                    countProp.intValue = 5;

                SerializedProperty offsetProp = newType.FindPropertyRelative("spawnOffset");
                if (offsetProp != null)
                    offsetProp.vector3Value = Vector3.zero;
            }
        }
        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Renders per-spawn assignment filters with toggles per enemy type.
    /// </summary>
    private void DrawSpawnAssignments(SerializedProperty spawnAssignmentsProperty, SerializedProperty enemyTypesProperty, Vector2Int[] spawnCoords, string[] spawnLabels)
    {
        EditorGUILayout.LabelField("Per-Spawner Filters", EditorStyles.boldLabel);
        if (spawnAssignmentsProperty == null)
        {
            EditorGUILayout.HelpBox("Spawn assignment data is missing on this wave.", MessageType.Error);
            return;
        }

        if (spawnCoords.Length == 0)
        {
            EditorGUILayout.HelpBox("No spawn nodes found on the assigned Grid3D. Paint enemy spawn cells to enable selection.", MessageType.Warning);
            return;
        }

        string[] enemyTypeLabels = BuildEnemyTypeLabels(enemyTypesProperty);
        if (enemyTypeLabels.Length == 0)
        {
            EditorGUILayout.HelpBox("Add at least one Enemy Type to enable per-spawner filters.", MessageType.Info);
            return;
        }

        spawnAssignmentsProperty.isExpanded = EditorGUILayout.Foldout(spawnAssignmentsProperty.isExpanded, "Assignments", true, foldoutStyle);
        if (!spawnAssignmentsProperty.isExpanded)
            return;

        EditorGUI.indentLevel++;
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(12f);
        EditorGUILayout.BeginVertical();
        int assignmentCount = spawnAssignmentsProperty.arraySize;
        for (int i = 0; i < assignmentCount; i++)
        {
            SerializedProperty assignmentProperty = spawnAssignmentsProperty.GetArrayElementAtIndex(i);
            SerializedProperty spawnNodeProperty = assignmentProperty.FindPropertyRelative("spawnNode");
            SerializedProperty allowedTypesProperty = assignmentProperty.FindPropertyRelative("allowedEnemyTypeIndices");

            EditorGUILayout.BeginVertical("box");
            assignmentProperty.isExpanded = EditorGUILayout.Foldout(assignmentProperty.isExpanded, $"Spawner {i + 1}", true, foldoutStyle);
            if (assignmentProperty.isExpanded)
            {
                EditorGUI.indentLevel++;
                DrawSpawnNodeDropdown(spawnNodeProperty, spawnCoords, spawnLabels, i);
                DrawAllowedEnemyTypes(allowedTypesProperty, enemyTypeLabels);
                EditorGUI.indentLevel--;
            }

            if (GUILayout.Button("Remove Spawner"))
            {
                spawnAssignmentsProperty.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                return;
            }
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Spawner Assignment"))
        {
            int newIndex = spawnAssignmentsProperty.arraySize;
            spawnAssignmentsProperty.InsertArrayElementAtIndex(newIndex);
            SerializedProperty newAssignment = spawnAssignmentsProperty.GetArrayElementAtIndex(newIndex);
            if (newAssignment != null)
            {
                SerializedProperty nodeProp = newAssignment.FindPropertyRelative("spawnNode");
                if (nodeProp != null && spawnCoords.Length > 0)
                    nodeProp.vector2IntValue = spawnCoords[0];

                SerializedProperty allowedProp = newAssignment.FindPropertyRelative("allowedEnemyTypeIndices");
                if (allowedProp != null)
                {
                    allowedProp.ClearArray();
                    allowedProp.InsertArrayElementAtIndex(0);
                    allowedProp.GetArrayElementAtIndex(0).intValue = 0;
                }
            }
        }
        if (GUILayout.Button("Clear Assignments"))
            spawnAssignmentsProperty.ClearArray();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// Draws a dropdown constrained to grid spawn cells.
    /// </summary>
    private void DrawSpawnNodeDropdown(SerializedProperty spawnNodeProperty, Vector2Int[] spawnCoords, string[] spawnLabels, int index)
    {
        if (spawnNodeProperty == null)
            return;

        Vector2Int currentValue = spawnNodeProperty.vector2IntValue;
        int selectedIndex = System.Array.IndexOf(spawnCoords, currentValue);
        if (selectedIndex < 0)
            selectedIndex = 0;

        int newIndex = EditorGUILayout.Popup($"Spawn Node #{index + 1}", selectedIndex, spawnLabels);
        if (newIndex >= 0 && newIndex < spawnCoords.Length)
            spawnNodeProperty.vector2IntValue = spawnCoords[newIndex];
    }

    /// <summary>
    /// Presents toggles for allowed enemy types on a specific spawner.
    /// </summary>
    private void DrawAllowedEnemyTypes(SerializedProperty allowedTypesProperty, string[] enemyTypeLabels)
    {
        if (allowedTypesProperty == null)
            return;

        HashSet<int> current = new HashSet<int>();
        int currentSize = allowedTypesProperty.arraySize;
        for (int i = 0; i < currentSize; i++)
        {
            SerializedProperty element = allowedTypesProperty.GetArrayElementAtIndex(i);
            current.Add(element.intValue);
        }

        for (int i = 0; i < enemyTypeLabels.Length; i++)
        {
            bool hasType = current.Contains(i);
            bool newValue = EditorGUILayout.Toggle(enemyTypeLabels[i], hasType);
            if (newValue && !hasType)
            {
                int newIndex = allowedTypesProperty.arraySize;
                allowedTypesProperty.InsertArrayElementAtIndex(newIndex);
                allowedTypesProperty.GetArrayElementAtIndex(newIndex).intValue = i;
            }
            else if (!newValue && hasType)
            {
                RemoveIndexFromProperty(allowedTypesProperty, i);
            }
        }
    }

    /// <summary>
    /// Removes all entries matching a value from a SerializedProperty array.
    /// </summary>
    private void RemoveIndexFromProperty(SerializedProperty listProperty, int value)
    {
        for (int i = listProperty.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
            if (element.intValue == value)
                listProperty.DeleteArrayElementAtIndex(i);
        }
    }

    /// <summary>
    /// Builds user-facing labels for enemy type arrays.
    /// </summary>
    private string[] BuildEnemyTypeLabels(SerializedProperty enemyTypesProperty)
    {
        if (enemyTypesProperty == null || enemyTypesProperty.arraySize == 0)
            return System.Array.Empty<string>();

        int count = enemyTypesProperty.arraySize;
        string[] labels = new string[count];
        for (int i = 0; i < count; i++)
        {
            SerializedProperty typeProperty = enemyTypesProperty.GetArrayElementAtIndex(i);
            SerializedProperty labelProperty = typeProperty.FindPropertyRelative("label");
            SerializedProperty definitionProperty = typeProperty.FindPropertyRelative("enemyDefinition");
            string label = labelProperty != null ? labelProperty.stringValue : string.Empty;
            if (string.IsNullOrWhiteSpace(label) && definitionProperty != null && definitionProperty.objectReferenceValue != null)
                label = definitionProperty.objectReferenceValue.name;

            labels[i] = string.IsNullOrWhiteSpace(label) ? $"Type {i + 1}" : label;
        }

        return labels;
    }

    /// <summary>
    /// Displays spawn node selectors for each wave entry.
    /// </summary>
    private void DrawSpawnNodesSelector(SerializedProperty spawnNodesProperty, Vector2Int[] spawnCoords, string[] spawnLabels)
    {
        if (spawnCoords.Length == 0)
        {
            EditorGUILayout.HelpBox("No spawn nodes available. Configure Grid3D spawn cells to enable this list.", MessageType.Warning);
            return;
        }

        int currentSize = spawnNodesProperty.arraySize;
        for (int i = 0; i < currentSize; i++)
        {
            SerializedProperty element = spawnNodesProperty.GetArrayElementAtIndex(i);
            Vector2Int currentValue = element.vector2IntValue;
            int selectedIndex = System.Array.IndexOf(spawnCoords, currentValue);
            if (selectedIndex < 0)
                selectedIndex = 0;
        }
    }

    /// <summary>
    /// Renders add and remove controls for waves.
    /// </summary>
    private void DrawWaveArrayControls(SerializedProperty wavesProperty, Vector2Int[] spawnCoords)
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Wave"))
            AppendWave(wavesProperty, spawnCoords);

        if (GUILayout.Button("Remove Last") && wavesProperty.arraySize > 0)
            wavesProperty.DeleteArrayElementAtIndex(wavesProperty.arraySize - 1);

        EditorGUILayout.EndHorizontal();
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Creates readable labels for grid spawn coordinates.
    /// </summary>
    private string[] BuildSpawnLabels(IReadOnlyList<Vector2Int> coords)
    {
        if (coords == null || coords.Count == 0)
            return new[] { "None" };

        string[] labels = new string[coords.Count];
        for (int i = 0; i < coords.Count; i++)
            labels[i] = $"({coords[i].x},{coords[i].y})";

        return labels;
    }

    /// <summary>
    /// Appends a new wave with sensible defaults and first spawn node preselected when available.
    /// </summary>
    private void AppendWave(SerializedProperty wavesProperty, Vector2Int[] spawnCoords)
    {
        int newIndex = wavesProperty.arraySize;
        wavesProperty.InsertArrayElementAtIndex(newIndex);
        SerializedProperty wave = wavesProperty.GetArrayElementAtIndex(newIndex);
        if (wave == null)
            return;

        SerializedProperty enemyTypesProperty = wave.FindPropertyRelative("enemyTypes");
        SerializedProperty spawnAssignmentsProperty = wave.FindPropertyRelative("spawnAssignments");
        SerializedProperty spawnNodesProperty = wave.FindPropertyRelative("spawnNodes");
        SerializedProperty spawnCadenceProperty = wave.FindPropertyRelative("spawnCadenceSeconds");
        SerializedProperty advanceModeProperty = wave.FindPropertyRelative("advanceMode");
        SerializedProperty advanceDelayProperty = wave.FindPropertyRelative("advanceDelaySeconds");

        if (enemyTypesProperty != null)
        {
            enemyTypesProperty.ClearArray();
            enemyTypesProperty.InsertArrayElementAtIndex(0);
            SerializedProperty typeProperty = enemyTypesProperty.GetArrayElementAtIndex(0);
            if (typeProperty != null)
            {
                SerializedProperty labelProp = typeProperty.FindPropertyRelative("label");
                if (labelProp != null)
                    labelProp.stringValue = string.Empty;

                SerializedProperty definitionProp = typeProperty.FindPropertyRelative("enemyDefinition");
                if (definitionProp != null)
                    definitionProp.objectReferenceValue = null;

                SerializedProperty modifiersProp = typeProperty.FindPropertyRelative("runtimeModifiers");
                if (modifiersProp != null)
                    modifiersProp.boxedValue = default(EnemyRuntimeModifiers);

                SerializedProperty countProp = typeProperty.FindPropertyRelative("enemyCount");
                if (countProp != null)
                    countProp.intValue = 5;

                SerializedProperty offsetProp = typeProperty.FindPropertyRelative("spawnOffset");
                if (offsetProp != null)
                    offsetProp.vector3Value = Vector3.zero;
            }
        }

        if (spawnAssignmentsProperty != null)
            spawnAssignmentsProperty.ClearArray();

        if (spawnCadenceProperty != null)
            spawnCadenceProperty.floatValue = 0.5f;
        if (advanceModeProperty != null)
            advanceModeProperty.enumValueIndex = (int)WaveAdvanceMode.FixedInterval;
        if (advanceDelayProperty != null)
            advanceDelayProperty.floatValue = 1f;

        if (spawnNodesProperty != null)
        {
            spawnNodesProperty.ClearArray();
            if (spawnCoords.Length > 0)
            {
                spawnNodesProperty.InsertArrayElementAtIndex(0);
                spawnNodesProperty.GetArrayElementAtIndex(0).vector2IntValue = spawnCoords[0];
            }
        }
    }

    /// <summary>
    /// Builds reusable GUI styles for headers and boxes.
    /// </summary>
    private void BuildStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 11;
        }

        if (foldoutStyle == null)
        {
            foldoutStyle = new GUIStyle(EditorStyles.foldout);
            foldoutStyle.fontStyle = FontStyle.Bold;
        }

        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(EditorStyles.helpBox);
            boxStyle.padding = new RectOffset(10, 10, 6, 8);
        }
    }

    /// <summary>
    /// Draws a simple section header label.
    /// </summary>
    private void DrawSectionHeader(string label)
    {
        EditorGUILayout.LabelField(label, headerStyle);
    }
    #endregion
    #endregion
}
