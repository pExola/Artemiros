using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NovoLevelGroup", menuName = "Artemiros/Criar Grupo de Nível")]
public class LevelGroup : ScriptableObject
{
    public List<LevelData> estagios; // Uma lista para arrastar os 3 LevelData de cada estágio
}