using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tree : MonoBehaviour {

    public GameObject child_node_prefab;
    public GameObject parent_node_prefab;

    public struct NodeData {
        public int type;
        public int child_index;
        public int parent_index;

        public NodeData(int type, int child_index, int parent_index) {
            this.type = type;
            this.child_index = child_index;
            this.parent_index = parent_index;
        }
    }

    public Color ColorForNodeType(int type) {
        switch (type) {
            case 0: return Color.red;
            case 1: return Color.yellow;
            case 2: return Color.green;
            case 3: return Color.magenta;
            case 4: return Color.blue;
            case 5: return Color.cyan;
            case 6: return Color.white;
            case 7: return Color.grey;
            case 8: return Color.black;
            case 9: return Color.clear;
            default: return Color.black;
        }
    }

    public const int n_children = 20;
    public const int n_types = 5;

    protected List<NodeData> nodes = new List<NodeData>();
    protected int current_node = 0;
    protected MeshRenderer[] visible_meshes = new MeshRenderer[n_children];
    protected ClickListener[] child_listeners = new ClickListener[n_children];
    protected GameObject[] child_nodes = new GameObject[n_children];
    protected ClickListener parent_listener;

    protected GameObject parent_node_instance;

    // Use this for initialization
    void Start() {
        // Add n_children initial nodes
        CreateSiblingGroup(current_node, null);

        // Initialize meshes
        float depth = 7;
        for (int i = 0; i < n_children; ++i) {

            float angle = 2 * Mathf.PI * i / (float) n_children;
            float radius = 3f;
            Vector3 position = new Vector3(radius * Mathf.Sin(angle), radius * Mathf.Cos(angle), depth);

            GameObject child_node_instance = (GameObject) Instantiate(child_node_prefab, position, Quaternion.identity);
            visible_meshes[i] = child_node_instance.GetComponent<MeshRenderer>();
            child_listeners[i] = child_node_instance.GetComponent<ClickListener>();
            child_nodes[i] = child_node_instance;

        }

        parent_node_instance = (GameObject) Instantiate(parent_node_prefab, new Vector3(0, 0, depth), Quaternion.identity);
        parent_listener = parent_node_instance.GetComponent<ClickListener>();

        UpdateMeshes(current_node);

        iTween.CameraFadeAdd();
    }

    // Update is called once per frame
    void Update() {

        // If a visible node is selected, descend into its sub-tree
        for (int i = 0; i < n_children; ++i) {

            if (child_listeners[i].Clicked()) {
                Descend(i);
                break;
            }

        }

        // If the parent node is selected, ascend to its super-tree
        if (parent_listener.Clicked()) {
            Ascend();
        }

        //RotateMeshes(Time.deltaTime);

    }

    void Ascend() {
    iTween.MoveTo(Camera.main.gameObject, new Hashtable(){
            {"position", parent_node_instance.transform.position - new Vector3(0, 0, 20)},
            {"time", 1f},
            {"oncomplete", "AfterZoomToParent"},
            {"oncompletetarget", gameObject }
        });

        iTween.CameraFadeTo(1, 1);
    }

    public void AfterZoomToParent() {
        // Move up to the super-tree of the current visible nodes
        int next_node = nodes[current_node].parent_index;

        if (next_node < 0) {

            // Create new nodes at the super-tree
            next_node = nodes.Count;
            CreateSiblingGroup(next_node, null);

            // Choose a random position for the parent among its siblings
            next_node += Random.Range(0, n_children);

            // Connect parent to children
            for (int i = 0; i < n_children; ++i) {
                SetParentIndex(current_node + i, next_node);
            }
            SetChildIndex(next_node, current_node);

        }

        // Ensure new current node is consistent with child types
        SetType(next_node, ParentNodeType(current_node));

        // Set current node to the first child in the current group of nodes
        current_node = next_node - (next_node % n_children);

        // Update meshes to match newly visible nodes
        UpdateMeshes(current_node);

        Camera.main.transform.position = parent_node_instance.transform.position;
        iTween.CameraFadeTo(0, .25f);

        iTween.MoveTo(Camera.main.gameObject, new Hashtable(){
            {"position", parent_node_instance.transform.position - new Vector3(0, 0, 8)},
        });
    }

    void Descend(int child) {
        iTween.MoveTo(Camera.main.gameObject, new Hashtable(){
            {"position", child_nodes[child].transform.position + new Vector3(0, 0, 1)},
            {"time", 1f},
            {"oncomplete", "AfterZoomToChild"},
            {"oncompleteparams", child },
            {"oncompletetarget", gameObject }
        });

        iTween.CameraFadeTo(1, 1);
    }

    public void AfterZoomToChild(int child) {
        // Move down to the sub-tree of the chosen child
        int next_node = nodes[current_node + child].child_index;

        if (next_node < 0) {

            // Create new nodes at the sub-tree
            int current_type = nodes[current_node + child].type;
            next_node = nodes.Count;

            do {

                CreateSiblingGroup(next_node, current_type);

            } while (ParentNodeType(next_node) != current_type);

            // Connect parent to children
            for (int i = 0; i < n_children; ++i) {
                SetParentIndex(next_node + i, current_node + child);
            }
            SetChildIndex(current_node + child, next_node);

        }

        current_node = next_node;

        // Update meshes to match newly visible nodes
        UpdateMeshes(current_node);

        // Add a slight shift to make it feel like we're coming from the direction
        // of the node we descended to
        Vector3 shift = child_nodes[child].transform.position - parent_node_instance.transform.position;

        Camera.main.transform.position = parent_node_instance.transform.position - new Vector3(0, 0, 100) - shift * 10;
        iTween.CameraFadeTo(0, .25f);

        iTween.MoveTo(Camera.main.gameObject, new Hashtable(){
            {"position", parent_node_instance.transform.position - new Vector3(0, 0, 8)},
        });
    }

    void CreateSiblingGroup(int start_index, int? ideal_type, int parent_index = -1) {

        // Ensure the node list has enough members to initialize n_children nodes
        // starting at the given position
        int nodes_to_add = start_index + n_children - nodes.Count;

        while (nodes_to_add-- > 0) {

            nodes.Add(new NodeData(-1, -1, -1));

        }

        // Initialize n_children nodes starting at the given position
        int past_type = Random.Range(0, n_types);
        for (int i = 0; i < n_children; ++i) {
            int t = Random.Range(0, n_types);

            // 50% change of using the type of the previous node
            if (Random.value > .5f) {
                t = past_type;
            }

            nodes[start_index + i] = new NodeData(t, -1, parent_index);

            past_type = t;

        }

    }

    void SetParentIndex(int index, int parent_index) {

        NodeData node = nodes[index];
        node.parent_index = parent_index;
        nodes[index] = node;

    }

    void SetChildIndex(int index, int child_index) {

        NodeData node = nodes[index];
        node.child_index = child_index;
        nodes[index] = node;

    }

    void SetType(int index, int type) {

        NodeData node = nodes[index];
        node.type = type;
        nodes[index] = node;

    }

    int ParentNodeType(int start_index) {

        // Count the number of nodes of each type
        int[] counts = new int[n_types];
        for (int i = 0; i < n_children; ++i) {

            int type = nodes[start_index + i].type;
            ++counts[type];

        }

        // Find the types with the highest and lowest counts
        int highest_type = 0;
        int absent_type = 0;
        int max_count = 0;
        int n_counts_one = 0;
        for (int i = 0; i < n_types; ++i) {

            int c = counts[i];

            if (c > max_count) {
                max_count = c;
                highest_type = i;
            }
            if (c == 0) {
                absent_type = i;
            }
            if (c == 1) {
                ++n_counts_one;
            }

        }

        // If all types but one have a count of one, result is the left-out type
        if (n_counts_one == n_types - 1) {
            return absent_type;
        }

        return highest_type;

    }

    void UpdateMeshes(int index) {

        for (int i = 0; i < visible_meshes.Length; ++i) {

            Color color = ColorForNodeType(nodes[index + i].type);

            MeshRenderer mesh = visible_meshes[i];
            mesh.material.color = color;
            mesh.transform.rotation = Quaternion.identity;

        }

    }

    void RotateMeshes(float delta_time) {

        float delta_angle = 10 * delta_time;
        foreach (var mesh in visible_meshes) {
            mesh.transform.Rotate(delta_angle, delta_angle, 0);
        }

    }

}
