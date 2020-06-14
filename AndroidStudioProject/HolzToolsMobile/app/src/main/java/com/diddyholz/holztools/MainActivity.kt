package com.diddyholz.holztools

import android.content.Context
import android.content.Intent
import android.graphics.Color
import android.net.ConnectivityManager
import android.net.NetworkInfo
import android.os.Bundle
import android.text.InputFilter
import android.view.Menu
import android.view.MenuItem
import android.view.View
import android.widget.EditText
import android.widget.Toast
import androidx.appcompat.app.ActionBarDrawerToggle
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.GravityCompat
import androidx.core.view.MenuCompat
import com.google.android.material.navigation.NavigationView
import kotlinx.android.synthetic.main.activity_main.*
import okhttp3.Call
import okhttp3.HttpUrl.Companion.toHttpUrlOrNull
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.Response
import java.util.concurrent.TimeUnit

/*
* TODO:
*  SPINNERMODE NOT WORKING WHEN CONNECTED TO PC
* */


class MainActivity : AppCompatActivity(), NavigationView.OnNavigationItemSelectedListener
{
    companion object
    {
        //constants for the mode
        const val TotalModes:Byte = 6

        const val ModeStatic:Byte = 0
        const val ModeCycle:Byte = 1
        const val ModeRainbow:Byte = 2
        const val ModeLightning:Byte = 3
        const val ModeOverlay:Byte = 4
        const val ModeSpinner:Byte = 5

        // constants for tcp codes
        const val TCPGETINFO = "GETINFO"
        const val TCPSETLED = "SETLED"
        const val TCPINVALIDCOMMAND = 400
        const val TCPNOCONNECTEDLEDS = 300
        const val TCPREQUESTOK = 200
        const val TCPLEDNOTFOUND = 404

        const val blockCharacterSet = ",&=@"

        val client = OkHttpClient.Builder().connectTimeout(200, TimeUnit.MILLISECONDS).build()

        var serverPort = 39769
        var selectedLedItem: LedItem? = null

        lateinit var activeMainActivity:MainActivity

        fun isNetworkOnline(): Boolean
        {
            val connectivityManager = activeMainActivity.getSystemService(Context.CONNECTIVITY_SERVICE) as ConnectivityManager
            val activeNetwork: NetworkInfo? = connectivityManager.activeNetworkInfo

            return activeNetwork?.isConnectedOrConnecting == true
        }

        fun sendGetRequest(targetIp: String, targetPort: Int, attributes: MutableList<HTTPAttribute>): String?
        {
            val urlBuilder = "http://$targetIp:$targetPort".toHttpUrlOrNull()!!.newBuilder()

            // add every attribute
            for(attr in attributes)
            {
                urlBuilder.addQueryParameter(attr.attrName, attr.attrValue)
            }

            urlBuilder.port(targetPort)

            val url = urlBuilder.build().toString()

            val request: Request = Request.Builder().url(url).addHeader("Connection", "close").build()

            val call: Call = client.newCall(request)

            val response: Response = call.execute()

            return response.body?.string()
        }

        fun sendDataToPC(item: LedItem) : Int?
        {
            val attrs = mutableListOf<HTTPAttribute>()

            var modeString = ""

            attrs.add(HTTPAttribute("Command", TCPSETLED))

            attrs.add(HTTPAttribute("LEDItem", item.hostLedName))

            // set the current mode as a string
            when(item.currentMode)
            {
                ModeStatic -> {
                    modeString = "Static"
                    attrs.add(HTTPAttribute("StaticBrightness", item.staticBrightness.toString()))
                    attrs.add(HTTPAttribute("StaticModeColor", "${Color.red(item.staticColor)},${Color.green(item.staticColor)},${Color.blue(item.staticColor)}"))
                }
                ModeCycle -> {
                    modeString = "Cycle"
                    attrs.add(HTTPAttribute("CycleBrightness", item.cycleBrightness.toString()))
                    attrs.add(HTTPAttribute("CycleSpeed", item.cycleSpeed.toString()))
                }
                ModeRainbow -> {
                    modeString = "Rainbow"
                    attrs.add(HTTPAttribute("RainbowBrightness", item.rainbowBrightness.toString()))
                    attrs.add(HTTPAttribute("RainbowSpeed", item.rainbowSpeed.toString()))
                }
                ModeLightning -> {
                    modeString = "Lightning"
                    attrs.add(HTTPAttribute("LightningBrightness", item.lightningBrightness.toString()))
                    attrs.add(HTTPAttribute("LightningModeColor", "${Color.red(item.lightningColor)},${Color.green(item.lightningColor)},${Color.blue(item.lightningColor)}"))
                }
                ModeOverlay -> {
                    modeString = "Color_Overlay"
                    attrs.add(HTTPAttribute("OverlaySpeed", item.overlaySpeed.toString()))
                    attrs.add(HTTPAttribute("OverlayDirection", item.overlayDirection.toString()))
                }
                ModeSpinner -> {
                    modeString = "Color_Spinner"
                    attrs.add(HTTPAttribute("SpinnerModeSpinnerColor", "${Color.red(item.spinnerSpinnerColor)},${Color.green(item.spinnerSpinnerColor)},${Color.blue(item.spinnerSpinnerColor)}"))
                    attrs.add(HTTPAttribute("SpinnerColorBrightness", item.spinnerColorBrightness.toString()))
                    attrs.add(HTTPAttribute("SpinnerSpeed", item.spinnerSpeed.toString()))
                    attrs.add(HTTPAttribute("SpinnerLength", item.spinnerLength.toString()))
                    attrs.add(HTTPAttribute("SpinnerModeBackgroundColor", "${Color.red(item.spinnerBackgroundColor)},${Color.green(item.spinnerBackgroundColor)},${Color.blue(item.spinnerBackgroundColor)}"))
                    attrs.add(HTTPAttribute("BackgroundColorBrightness", item.backgroundColorBrightness.toString()))
                }
                else -> {
                    modeString = "Static"
                    attrs.add(HTTPAttribute("StaticBrightness", item.staticBrightness.toString()))
                    attrs.add(HTTPAttribute("StaticModeColor", "${Color.red(item.staticColor)},${Color.green(item.staticColor)},${Color.blue(item.staticColor)}"))
                }
            }

            attrs.add(HTTPAttribute("LEDMode", modeString))

            return sendGetRequest(item.ip, item.tcpServerPort, attrs)?.toInt()
        }

        val nameFilter =
            InputFilter { source, start, end, dest, dstart, dend ->
                if (source != null && blockCharacterSet.contains("" + source)) {
                    ""
                } else null
            }
    }

    val selectModeFragmentTag = "FRAGMENT_SELECTMODE"

    override fun onCreate(savedInstanceState: Bundle?)
    {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        activeMainActivity = this

        setSupportActionBar(toolbar)

        val drawerToggle = ActionBarDrawerToggle(
            this,
            mainDrawerLayout,
            toolbar,
            R.string.navigation_drawer_open,
            R.string.navigation_drawer_close
        )
        mainDrawerLayout.addDrawerListener(drawerToggle)
        drawerToggle.syncState()
        MenuCompat.setGroupDividerEnabled(navView.menu, true)

        navView.setNavigationItemSelectedListener(this)

        if (!isNetworkOnline())
            noConnectionAlert.visibility = View.VISIBLE

        // create a menu item for all leds
        for(ledItem in LedItem.allItems)
        {
            ledItem.createNavViewMenuItem()
        }

        if(selectedLedItem != null)
        {
            navView.setCheckedItem(selectedLedItem!!.menuItemId)
            toolbar.title = selectedLedItem!!.customName
        }

        if(savedInstanceState == null)
        {
            supportFragmentManager.beginTransaction().replace(R.id.mainFragmentContainer, NoLedsFragment()).commit()
        }
    }

    fun setSelectedLedItem(item: LedItem)
    {
        selectedLedItem = item

        // set the correct mode
        (supportFragmentManager.findFragmentByTag(selectModeFragmentTag) as? SelectModeFragment)?.setViewPagerItem(selectedLedItem!!.currentMode.toInt())
    }

    override fun onCreateOptionsMenu(menu: Menu?): Boolean
    {
        menuInflater.inflate(R.menu.menu_toolbar, menu)
        return true
    }

    override fun onOptionsItemSelected(item: MenuItem): Boolean
    {
        if(selectedLedItem == null)
            return false

        var newIntent = Intent(this, LedPropertiesActivity().javaClass)
        newIntent.putExtra("LedItemName", selectedLedItem!!.customName)
        startActivity(newIntent)

        return true
    }

    override fun onNavigationItemSelected(item: MenuItem): Boolean
    {
        //choose what happens when what menuitem is pressed
        when(item.itemId)
        {
            R.id.menuSettings -> supportFragmentManager.beginTransaction().replace(R.id.mainFragmentContainer, SettingsFragment()).commit()
            R.id.menuAbout -> supportFragmentManager.beginTransaction().replace(R.id.mainFragmentContainer, AboutFragment()).commit()
            R.id.menuAddItem -> {
                LedItem()
                supportFragmentManager.beginTransaction().replace(R.id.mainFragmentContainer, SelectModeFragment(), selectModeFragmentTag).commit()
                toolbar.title = selectedLedItem!!.customName
            }
            else -> {
                //show the modeSelectFragment if an LEDItem is clicked
                supportFragmentManager.beginTransaction().replace(R.id.mainFragmentContainer, SelectModeFragment(), selectModeFragmentTag).commit()

                var foundLedItem = false

                //set the selected ledItem
                for(leditem in LedItem.allItems)
                {
                    if(leditem.customName == item.title)
                    {
                        setSelectedLedItem(leditem)
                        foundLedItem = true
                    }
                }

                //display a toast if the leditem couldn't be found
                if(foundLedItem)
                    toolbar.title = selectedLedItem!!.customName
                else
                    Toast.makeText(this, "Couldn't find LedItem!", Toast.LENGTH_SHORT).show()
            }
        }

        mainDrawerLayout.closeDrawer(GravityCompat.START)
        return true
    }

    override fun onBackPressed()
    {
        if(mainDrawerLayout.isDrawerOpen(GravityCompat.START))
            mainDrawerLayout.closeDrawer(GravityCompat.START)
        else
            super.onBackPressed()
    }
}
