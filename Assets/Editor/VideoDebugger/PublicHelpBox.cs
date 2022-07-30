using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PublicHelpBox : HelpBox
{
    public new class UxmlFactory : UxmlFactory<PublicHelpBox, HelpBox.UxmlTraits> { }
}
