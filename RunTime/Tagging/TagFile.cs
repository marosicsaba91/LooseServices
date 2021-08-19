﻿using UnityServiceLocator;
using UnityEngine;

[CreateAssetMenu(fileName = "Tag", menuName = "Loose Services/Tag File")]
public class TagFile : ScriptableObject, ITag
{
    [SerializeField] Color color;
    public Color Color => color;
    public string Name => name;
    public object TagObject => this;
}
