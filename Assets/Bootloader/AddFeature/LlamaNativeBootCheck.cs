using System.Collections;
using UnityEngine;

public class LlamaNativeBootCheck : AndroidNativeLibraryBootCheck
{
    // Inspectorで libraryNameWithoutPrefix を llama_unity_shim にして使う。
    // 専用クラスとして置いておくことで、BootScene上で役割が分かりやすくなる。
}
