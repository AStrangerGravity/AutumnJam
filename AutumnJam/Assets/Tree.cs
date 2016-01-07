using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class Tree : MonoBehaviour {
    // (0...1): Chance of next type in generated layer following the previous generated type.
    private const float HOMOGENITY = .8f;

    public const int n_children = 8;
    public const int n_types = 10;
    protected ClickListener[] child_listeners = new ClickListener[n_children];

    public GameObject child_node_prefab;
    protected GameObject[] child_nodes = new GameObject[n_children];
    protected int current_node;

    private readonly NodeType[] node_types = {
        new NodeType {color = Color.green, weight = 30f},
        new NodeType {color = Color.blue, weight = 8f},
        new NodeType {color = Color.yellow, weight = 0f},
        new NodeType {color = Color.red, weight = 0f},
        new NodeType {color = Color.magenta, weight = 0f},
        new NodeType {color = Color.cyan, weight = .3f},
        new NodeType {color = Color.white, weight = .6f},
        new NodeType {color = Color.grey, weight = 0f},
        new NodeType {color = Color.black, weight = .3f},
        new NodeType {color = Color.clear, weight = 0f},
    };

    protected List<NodeData> nodes = new List<NodeData>();
    protected ClickListener parent_listener;

    protected GameObject parent_node_instance;
    public GameObject parent_node_prefab;
    protected MeshRenderer[] visible_meshes = new MeshRenderer[n_children];

    // Use this for initialization
    private void Start() {
        // Add n_children initial nodes
        CreateSiblingGroup(current_node, null);

        // Initialize meshes
        const float depth = 7;

        // +1 because we don't place a node at the center
        int child_index = 0;
        for (int i = 0; i < n_children + 1; ++i) {
            //float angle = 2 * Mathf.PI * i / (float) n_children;
            //float radius = 3f;
            //Vector3 position = new Vector3(radius * Mathf.Sin(angle), radius * Mathf.Cos(angle), depth);
            const float spacing = 1.5f;
            int row_size = Mathf.CeilToInt(Mathf.Sqrt(n_children));
            float offset = -(spacing * (row_size - 1)) / 2f;
            int x = i % row_size;
            int y = i / row_size;

            if (x == row_size / 2 && y == row_size / 2) {
                continue;
            }

            Vector3 position = new Vector3(offset + spacing * x, offset + spacing * y, depth);

            GameObject child_node_instance =
                (GameObject) Instantiate(child_node_prefab, position, Quaternion.identity);
            visible_meshes[child_index] = child_node_instance.GetComponent<MeshRenderer>();
            child_listeners[child_index] = child_node_instance.GetComponent<ClickListener>();
            child_nodes[child_index] = child_node_instance;
            child_index++;
        }

        parent_node_instance = (GameObject)
            Instantiate(parent_node_prefab, new Vector3(0, 0, depth), Quaternion.identity);
        parent_listener = parent_node_instance.GetComponent<ClickListener>();

        UpdateMeshes(current_node);

        iTween.CameraFadeAdd();
    }

    // Update is called once per frame
    private void Update() {
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

    private void Ascend() {
        iTween.MoveTo(Camera.main.gameObject, new Hashtable {
            {"position", parent_node_instance.transform.position - new Vector3(0, 0, 20)},
            {"time", 1f},
            {"oncomplete", "AfterZoomToParent"},
            {"oncompletetarget", gameObject}
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
        current_node = next_node - next_node % n_children;

        // Update meshes to match newly visible nodes
        UpdateMeshes(current_node);

        Camera.main.transform.position = parent_node_instance.transform.position;
        iTween.CameraFadeTo(0, .25f);

        iTween.MoveTo(Camera.main.gameObject, new Hashtable {
            {"position", parent_node_instance.transform.position - new Vector3(0, 0, 8)}
        });
    }

    private void Descend(int child) {
        iTween.MoveTo(Camera.main.gameObject, new Hashtable {
            {"position", child_nodes[child].transform.position + new Vector3(0, 0, 1)},
            {"time", 1f},
            {"oncomplete", "AfterZoomToChild"},
            {"oncompleteparams", child},
            {"oncompletetarget", gameObject}
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
        Vector3 shift = child_nodes[child].transform.position -
                        parent_node_instance.transform.position;

        Camera.main.transform.position = parent_node_instance.transform.position -
                                         new Vector3(0, 0, 100) - shift * 10;
        iTween.CameraFadeTo(0, .25f);

        iTween.MoveTo(Camera.main.gameObject, new Hashtable {
            {"position", parent_node_instance.transform.position - new Vector3(0, 0, 8)}
        });
    }

    private void CreateSiblingGroup(int start_index, int? ideal_type, int parent_index = -1) {
        // Ensure the node list has enough members to initialize n_children nodes
        // starting at the given position
        int nodes_to_add = start_index + n_children - nodes.Count;

        while (nodes_to_add-- > 0) {
            nodes.Add(new NodeData(-1, -1, -1));
        }

        // Initialize n_children nodes starting at the given position
        int past_type = GetRandomWeightedNodeType();
        for (int i = 0; i < n_children; ++i) {
            int t = GetRandomWeightedNodeType();

            // 50% change of using the type of the previous node
            if (Random.value < HOMOGENITY) {
                t = past_type;
            }

            nodes[start_index + i] = new NodeData(t, -1, parent_index);

            past_type = t;
        }
    }

    private int GetRandomWeightedNodeType() {
        float cumulative = node_types.Select(item => item.weight).Sum();

        float choice = Random.value * cumulative;
        for (int i = 0; i < node_types.Length; i++) {
            if (choice <= node_types[i].weight) {
                return i;
            }
            choice -= node_types[i].weight;
        }

        throw new AssertionException("Algorithm should always terminate. Error.", "");
    }

    private void SetParentIndex(int index, int parent_index) {
        NodeData node = nodes[index];
        node.parent_index = parent_index;
        nodes[index] = node;
    }

    private void SetChildIndex(int index, int child_index) {
        NodeData node = nodes[index];
        node.child_index = child_index;
        nodes[index] = node;
    }

    private void SetType(int index, int type) {
        NodeData node = nodes[index];
        node.type = type;
        nodes[index] = node;
    }

    private int ParentNodeType(int start_index) {
        // Count the number of nodes of each type
        var counts = new int[n_types];
        for (int i = 0; i < n_children; ++i) {
            int type = nodes[start_index + i].type;
            ++counts[type];
        }

        // var test = nodes.GroupBy(n => n.type, n => 1, (key, g) => g.Sum());
        // test.Ma

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

    private void UpdateMeshes(int index) {
        for (int i = 0; i < visible_meshes.Length; ++i) {
            Color color = node_types[nodes[index + i].type].color;

            MeshRenderer mesh = visible_meshes[i];
            mesh.material.color = color;
            mesh.transform.rotation = Quaternion.identity;
        }
    }

    private void RotateMeshes(float delta_time) {
        float delta_angle = 10 * delta_time;
        foreach (MeshRenderer mesh in visible_meshes) {
            mesh.transform.Rotate(delta_angle, delta_angle, 0);
        }
    }

    private struct NodeType {
        public Color color;

        // The relative likelihood that this type is chosen
        public float weight;
    }

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
}
