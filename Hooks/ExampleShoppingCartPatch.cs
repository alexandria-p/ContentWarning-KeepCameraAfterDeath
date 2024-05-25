using System;

namespace KeepCameraAfterDeath.Patches;

public class ExampleShoppingCartPatch
{
    internal static void Init()
    {
        /*
         *  Subscribe with 'On.Namespace.Type.Method += CustomMethod;' for each method you're patching.
         *  Or if you are writing an ILHook, use 'IL.' instead of 'On.'
         *  Note that not all types are in a namespace, especially in Unity games.
         */

        On.ShoppingCart.AddItemToCart += ShoppingCart_AddItemToCart;
    }

    private static void ShoppingCart_AddItemToCart(On.ShoppingCart.orig_AddItemToCart orig, ShoppingCart self, ShopItem item)
    {
        // Call the Trampoline for the Original method or another method in the Detour Chain if any exist
        orig(self, item);

        /*
         * Adding a random value to the visible price of the shopping cart typically is slightly
         * complicated due to the private setter of the CartValue property. However, as we have publicized the
         * game assembly, we do not have to worry about it, since it now is public.
         */
        self.CartValue += new Random().Next(0, 100);
    }
}
