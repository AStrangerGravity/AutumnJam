using UnityEngine;

public class ClickListener : MonoBehaviour {

  protected bool clicked = false;

  public void OnMouseDown() {

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
