package com.diddyholz.holztools

import android.graphics.Color
import android.view.Menu
import android.view.MenuItem
import kotlinx.android.synthetic.main.activity_main.*
import kotlinx.coroutines.selects.select

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
    var currentMode:Byte = MainActivity.ModeStatic
    var ledCount:Byte = 0
    var dPin:Byte = 0
    var rPin:Byte = 0
    var gPin:Byte = 0
    var bPin:Byte = 0

    var menuItemId = 0

    var tcpServerPort = 39769

    var ip = ""
    var customName = ""
        set(value) {
            //set the title of the corresponding menu item
            MainActivity.activeMainActivity.navView.menu.findItem(menuItemId)?.title = value

            field = value
        }

    var hostLedName = ""

    var useAdvancedIpSettings = false
    var isConnectedToPC = false

    // all mode variables
    var staticBrightness = 255
    var cycleBrightness = 255
    var rainbowBrightness = 255
    var lightningBrightness = 255
    var cycleSpeed = 0
    var rainbowSpeed = 0
    var overlaySpeed = 0
    var overlayDirection = 0
    var spinnerSpeed = 0
    var spinnerLength = 10
    var spinnerColorBrightness = 255
    var backgroundColorBrightness = 255
    var staticColor = Color.parseColor("#FF0000")
    var lightningColor = Color.parseColor("#FF0000")
    var spinnerSpinnerColor = Color.parseColor("#FF0000")
    var spinnerBackgroundColor = Color.parseColor("#0000FF")

    constructor()
    {
        id = IdCounter() // get an incremented ID
        allItems.add(this) // add the new LedItem to the list of all items
        customName = "LED$id"

        // create the menu item
        val temp = createNavViewMenuItem()

        //set the new leditem as selected
        MainActivity.activeMainActivity.navView.setCheckedItem(temp)
        MainActivity.activeMainActivity.setSelectedLedItem(this)
    }

    fun deleteLedItem()
    {
        allItems.remove(this)
        MainActivity.activeMainActivity.navView.menu.removeItem(menuItemId)

        if(MainActivity.selectedLedItem == this)
        {
            // show the no leds added alert if there are no leds left
            if(allItems.count() == 0)
            {
                MainActivity.activeMainActivity.supportFragmentManager.beginTransaction().replace(R.id.mainFragmentContainer, NoLedsFragment()).commitAllowingStateLoss()
                MainActivity.activeMainActivity.toolbar.title = "HolzTools"
                return
            }

            MainActivity.activeMainActivity.setSelectedLedItem(allItems[0])
            MainActivity.activeMainActivity.toolbar.title = MainActivity.selectedLedItem!!.customName
            MainActivity.activeMainActivity.navView.setCheckedItem(MainActivity.selectedLedItem!!.menuItemId)
        }
    }

    fun createNavViewMenuItem() : MenuItem
    {
        menuItemId = Menu.FIRST + id

        val menuItem = MainActivity.activeMainActivity.navView.menu.add(R.id.menuLedItemGroup, menuItemId, 0, customName)
        MainActivity.activeMainActivity.navView.menu.setGroupCheckable(R.id.menuLedItemGroup, true, true)

        return menuItem
    }
}