using UnityEngine;
using System.Collections;

public class ClickListener : MonoBehaviour {

  protected bool clicked = false;

  void OnMouseDown() {

    clicked = true;

  }

  public bool Clicked(bool reset = true) {

    bool c = clicked;

    if (reset) {
      clicked = false;
    }

    return c;

  }
}
