using AnKuchen.KuchenList;
using AnKuchen.Layout;
using AnKuchen.Map;
using UnityEngine;
using UnityEngine.UI;

namespace AkyuiUnity.Sample
{
    public class Sample : MonoBehaviour
    {
        [SerializeField] private UICache uiCache = default;

        public void Start()
        {
            var ui = new UiElements(uiCache);

            using (var editor = ui.List.Edit())
            {
                for (var i = 0; i < 30; ++i)
                {
                    editor.Contents.Add(new UIFactory<ListItem>(x =>
                    {
                        using (var e = Layouter.LeftToRight(x.Item))
                        {
                            e.Create();
                            e.Create();
                            e.Create();
                        }
                    }));
                }
            }
        }
    }

    public class UiElements : IMappedObject
    {
        public IMapper Mapper { get; private set; }
        public GameObject Root { get; private set; }
        public VerticalList<ListItem> List { get; private set; }

        public UiElements()
        {
        }

        public UiElements(IMapper mapper)
        {
            Initialize(mapper);
        }

        public void Initialize(IMapper mapper)
        {
            Mapper = mapper;
            Root = mapper.Get();
            List = new VerticalList<ListItem>(
                mapper.Get<ScrollRect>("Scroll"),
                mapper.GetChild<ListItem>("Line")
            );
        }
    }

    public class ListItem : IMappedObject
    {
        public IMapper Mapper { get; private set; }
        public GameObject Root { get; private set; }
        public Item Item { get; private set; }

        public ListItem()
        {
        }

        public ListItem(IMapper mapper)
        {
            Initialize(mapper);
        }

        public void Initialize(IMapper mapper)
        {
            Mapper = mapper;
            Root = mapper.Get();
            Item = mapper.GetChild<Item>("Item");
        }
    }

    public class Item : IMappedObject
    {
        public IMapper Mapper { get; private set; }
        public GameObject Root { get; private set; }

        public Item()
        {
        }

        public Item(IMapper mapper)
        {
            Initialize(mapper);
        }

        public void Initialize(IMapper mapper)
        {
            Mapper = mapper;
            Root = mapper.Get();
        }
    }
}
