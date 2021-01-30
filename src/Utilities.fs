[<AutoOpen>]
module Utilities
open System.Xml.Linq

module Option =
    let collect f =
        Option.map f >> Option.flatten

type XNode with
    member this.Ancestors(name) = this.Ancestors(XName.Get name)
    member this.ElementsAfterSelf(name) = this.ElementsAfterSelf(XName.Get name)
    member this.ElementsBeforeSelf(name) = this.ElementsBeforeSelf(XName.Get name)    

type XContainer with
    member this.Descendants(name) = this.Descendants(XName.Get name)
    member this.Element(name) = this.Element(XName.Get name)
    member this.Elements(name) = this.Elements(XName.Get name)

type XElement with
    member this.AncestorsAndSelf(name) = this.AncestorsAndSelf(XName.Get name)
    member this.Attribute(name) = this.Attribute(XName.Get name)
    member this.Attributes(name) = this.Attributes(XName.Get name)
    member this.DescendantsAndSelf(name) = this.DescendantsAndSelf(XName.Get name)
    member this.SetAttributeValue(name, value) = this.SetAttributeValue(XName.Get name, value)
    member this.SetElementValue(name, value) = this.SetElementValue(XName.Get name, value)
