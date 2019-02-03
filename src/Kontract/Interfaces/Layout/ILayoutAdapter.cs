using System.Collections.Generic;
using Kontract.Interfaces.Common;

namespace Kontract.Interfaces.Layout
{
    /// <summary>
    /// This is the layout adapter interface for creating layout format plugins.
    /// </summary>
    public interface ILayoutAdapter : IPlugin
    {
        /// <summary>
        /// The list of items provided by the layout adapter to the UI.
        /// </summary>
        IList<LayoutItem> Items { get; }
    }

    ///// <summary>
    ///// This interface allows the layout adapter to add new Items through the UI.
    ///// </summary>
    //public interface IAddItems
    //{
    //    /// <summary>
    //    /// Creates a new Item and allows the plugin to provide its derived type.
    //    /// </summary>
    //    /// <returns>layoutItem or a derived type.</returns>
    //    layoutItem NewItem();

    //    /// <summary>
    //    /// Adds a newly created Item to the file and allows the plugin to perform any required adding steps.
    //    /// </summary>
    //    /// <param name="Item"></param>
    //    /// <returns>True if the Item was added, False otherwise.</returns>
    //    bool AddItem(layoutItem Item);
    //}

    ///// <summary>
    ///// This interface allows the layout afapter to rename Items through the UI making use of the NameList.
    ///// </summary>
    //public interface IRenameItems
    //{
    //    /// <summary>
    //    /// Renames an Item and allows the plugin to perform any required renaming steps.
    //    /// </summary>
    //    /// <param name="Item">The Item being renamed.</param>
    //    /// <param name="name">The new name to be assigned.</param>
    //    /// <returns>True if the Item was renamed, False otherwise.</returns>
    //    bool RenameItem(layoutItem Item, string name);
    //}

    ///// <summary>
    ///// This interface allows the layout adapter to delete Items through the UI.
    ///// </summary>
    //public interface IDeleteItems
    //{
    //    /// <summary>
    //    /// Deletes an Item and allows the plugin to perform any required deletion steps.
    //    /// </summary>
    //    /// <param name="Item">The Item to be deleted.</param>
    //    /// <returns>True if the Item was successfully deleted, False otherwise.</returns>
    //    bool DeleteItem(layoutItem Item);
    //}

    ///// <summary>
    ///// Items provide an extended properties dialog?
    ///// </summary>
    //public interface IItemsHaveExtendedProperties
    //{
    //    // TODO: Figure out how to best implement this feature with WPF.
    //    /// <summary>
    //    /// Opens the extended properties dialog for an Item.
    //    /// </summary>
    //    /// <param name="Item">The Item to view and/or edit extended properties for.</param>
    //    /// <returns>True if changes were made, False otherwise.</returns>
    //    bool ShowItemProperties(layoutItem Item);
    //}

    /// <summary>
    /// The base layout item class.
    /// </summary>
    public class LayoutItem
    {
        ///// <summary>
        ///// The entry's name.
        ///// </summary>
        //[XmlAttribute("name")]
        //public virtual string Name { get; set; } = string.Empty;

        ///// <summary>
        ///// Stores the original layout for the entry.
        ///// </summary>
        //[XmlElement("original")]
        //public virtual string Originallayout { get; set; } = string.Empty;

        ///// <summary>
        ///// Stores the edited layout for the entry.
        ///// </summary>
        //[XmlElement("edited")]
        //public virtual string Editedlayout { get; set; } = string.Empty;

        ///// <summary>
        ///// Stores the note layout for the entry.
        ///// </summary>
        //[XmlElement("notes")]
        //public virtual string Notes { get; set; } = string.Empty;

        ///// <summary>
        ///// Limits the allowed layout length that the entry can contain.
        ///// 0 for unlimited.
        ///// </summary>
        //[XmlAttribute("max_length")]
        //public virtual int MaxLength { get; set; } = 0;

        ///// <summary>
        ///// Determines whether this entry can be edited.
        ///// </summary>
        //[XmlIgnore]
        //public virtual bool CanEdit { get; } = true;
    }
}
