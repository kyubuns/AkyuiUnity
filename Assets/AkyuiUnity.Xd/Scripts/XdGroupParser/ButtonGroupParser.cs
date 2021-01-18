using System.Linq;
using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class ButtonGroupParser : IXdGroupParser
    {
        public bool Is(XdObjectJson instanceObject, XdObjectJson symbolObject)
        {
            var interactions = symbolObject.Meta?.Ux?.Interactions ?? new XdInteractionJson[] { };
            return interactions.Any(x => x.Enabled && x.Data.Interaction.TriggerEvent == "hover");
        }

        public IComponent[] Render(XdObjectJson instanceObject, XdObjectJson symbolObject)
        {
            Debug.Log("Render");
            return new IComponent[]
            {
                new ButtonComponent(0)
            };
        }
    }
}