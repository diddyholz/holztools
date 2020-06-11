package com.diddyholz.holztools

import android.view.Menu
import android.view.MenuItem
import kotlinx.android.synthetic.main.activity_main.*

class LedItem
{
    companion object
    {
        private var currentId:Byte = 0

        var allItems = mutableListOf<LedItem>()

        fun IdCounter():Byte
        {
            currentId++
            return currentId
        }
    }

    var id:Byte = 0
    var type:Byte = 0
    var ledCount:Byte = 0
    var dPin:Byte = 0
    var rPin:Byte = 0
    var gPin:Byte = 0
    var bPin:Byte = 0
    var isConnectedToPC = false

    var menuItemId = 0

    var ip = ""
    var customName = ""
        set(value) {
            //set the title of the corresponding menu item
            MainActivity.activeMainActivity.navView.menu.findItem(menuItemId)?.title = value

            field = value
        }

    constructor()
    {
        id = IdCounter() // get an incremented ID
        allItems.add(this) // add the new LedItem to the list of all items
        customName = "LED$id"

        menuItemId = Menu.FIRST + id

        var temp = MainActivity.activeMainActivity.navView.menu.add(R.id.menuLedItemGroup, menuItemId, 0, customName)
        MainActivity.activeMainActivity.navView.menu.setGroupCheckable(R.id.menuLedItemGroup, true, true)

        //set the new leditem as selected
        MainActivity.activeMainActivity.navView.setCheckedItem(temp)
        MainActivity.activeMainActivity.selectedLedItem = this
    }
}