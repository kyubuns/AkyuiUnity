# AkyuiUnity / AkyuiUnity.Xd Manual

***Read this document in other languages: [日本語](https://github.com/kyubuns/AkyuiUnity/blob/main/Manual/Manual_ja.md)***

---

### The quickstart on YouTube

[![](https://img.youtube.com/vi/bJC9ueWZp28/0.jpg)](https://www.youtube.com/watch?v=bJC9ueWZp28)8

https://www.youtube.com/watch?v=bJC9ueWZp28

---

## How to use

## Initial setup

- In PackageManager, import the following two items.
  - AkyuiUnity `https://github.com/kyubuns/AkyuiUnity.git?path=Assets/AkyuiUnity`
  - AkyuiUnity.Xd `https://github.com/kyubuns/AkyuiUnity.git?path=Assets/AkyuiUnity.Xd`
- Select Assets > Create > AkyuiXd > XdImportSettings and create a configuration file.
  - By tweaking these settings, you can decide how to import the XD for each project.
  - It has a powerful customization feature called "triggers". We'll talk about it later.


### How to create an XD file

- The Mark for Export flag in Artboard itself determines which Artboard will be exported.
- Special rules are summarized in [here](https://github.com/kyubuns/AkyuiUnity/blob/main/Manual/Manual_en.md#xd-conversion-rules).
- See [here](https://github.com/kyubuns/AkyuiUnity/blob/main/Manual/Manual_en.md#unsupported-item) for functions that cannot be used. for features that are not available.


### How to import

- In the Inspector of the XdImportSettings created in "First Time Setup", drag and drop the XD file to the place where it says "Drop xd".
- After the second time, you can import the file from the history with one button.


### Triggers

- This function allows you to customize how XD files are dropped into Prefab.
- For example, you can set whether to use uGUI's Text or TextMeshPro for text, whether to create a SpriteAtlas, and if so, where to create it.
- The list of triggers is [here](https://github.com/kyubuns/AkyuiUnity/blob/main/Manual/Manual_en.md#trigger-list) for a list of triggers.


### How to update

- Update the hash in Packages/packages-lock.json.
  - Unity, please prepare a way to update it in a better way.


---
## Xd Import Settings

### Prefab Output Path

- The path to output the Prefab.
- `{name}` will be replaced with the Artboard name.

### Asset Output Directory Path

- The path to the directory containing the images used in the Prefab.
- `{name}` will be replaced with the name of Artboard.

### Meta Output Path

- The path to output Meta files for Prefab.
  - Meta files do not need to be included in the build, but should be saved in a location visible to UnityEditor so that the information can be used when re-importing.
- `{name}` will be replaced with the Artboard name.

### Font Directory Path

- Font directory path to use when creating Prefabs.

### Triggers

### Sprite Save Scale

- Specifies how many times the size of the image used for the Prefab should be saved in XD.
- The larger the size, the larger the Sprite will be and the better it will be saved.

### Reimport Layout

- Reimports the layout of the prefs, even if there are no changes in XD.
  - This is useful if you have changed the settings in XdImportSettings and want them to be reflected.

### Reimport Asset

- Force to reimport the image to be used for the Prefab even if there are no changes in XD.
  - This is useful when you have changed the settings in XdImportSettings and want them to be reflected.

### Object Parsers

### Group Parsers

### Xd Triggers

### Akyui Output Path

- The output path of the Akyui (UI structure definition) file.
- If left blank, it will be invalid.


---
## Recommended usage

### Prefab handling

- Manually modifying the generated Prefab is not recommended.
  - Changes will be lost when you update XD and import it again.
  - Consider using Triggers if you want to perform specific operations.


### Connecting to [AnKuchen](https://github.com/kyubuns/AnKuchen)

- By using [AnKuchen](https://github.com/kyubuns/AnKuchen), you can easily manipulate the generated UI from a script.
- After importing AnKuchen and generating Prefab, you can use Trigger to give UICacheComponent automatically.
- (ToDo)
  - Specific usage


---
## XD conversion rules

### Naming

If you put the following at the end of the object's name, the component will be pasted on Unity as well.

#### `*Button`

- Button

#### `*Toggle`

- Toggle

#### `*Scrollbar`

- Scrollbar

#### `*Spacer`

- If you put an object named "Spacer" under Scroll, you can specify padding.

#### `*InputField`

- InputField

### Parameters

If you put @~ at the end of the object's name, you can get the following effect.

#### `@Placeholder`

- Do not export the image, only keep the position.

#### `@MultiItems`

- Only valid for Groups with Scroll.
- Expanded elements will be grandchildren instead of children.

#### `@Vector`

- When all children are vector data, import the group as one image into Unity.

#### `@Pivot`

- The center of that object becomes the origin of that group.

#### `@NoSlice`

- disable slice by Auto9Slicer


### Artboard Parameters

If you put @~ at the end of the artboard's name, you can get the following effect.

#### `@Expand`

- Make each object in the root a separate Prefab.


---
## Unsupported item

### Pending

#### State

- How far and what to reproduce.

#### Shadow

#### Blur

### No plan

#### Angular Gradient

- I don't do SVG output from XD because it's not supported.

#### 3D Transforms

- Unity can reproduce this, but I don't want to put it in Akyui, so I won't do it.

#### Blend Mode

- I can't think of a generic way to reproduce this in Unity, so I won't do it.


---
## Trigger List

ToDo


---
## Feedback

Please send us your feedback!

- [github issue](https://github.com/kyubuns/AkyuiUnity/issues) (BugReport only)
- [github discussion](https://github.com/kyubuns/AkyuiUnity/discussions)
- twitter [HashTag #akyui](https://twitter.com/search?q=%23akyui) or reply to [@kyubuns](https://twitter.com/kyubuns)!
- [MessageForm](https://kyubuns.dev/message.html)


---
## Buy me a coffee

Are you enjoying save time?  
Buy me a coffee if you love my code!  
https://www.buymeacoffee.com/kyubuns

## "I used it for this game!"

I'd be happy to receive reports like "I used it for this game!"  
Please contact me by email, twitter or any other means.  
(This library is MIT licensed, so reporting is NOT mandatory.)  
[Message Form](https://kyubuns.dev/message.html)

https://kyubuns.dev/
