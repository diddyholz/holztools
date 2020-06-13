package com.diddyholz.holztools

import android.content.Context
import android.content.Intent
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
        const val TCPINVALIDCOMMAND = 400
        const val TCPNOCONNECTEDLEDS = 300
        const val TCPREQUESTOK = 200

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

        fun sendGetRequest(targetIp: String, targetPort: String, attributes: MutableList<HTTPAttribute>): String?
        {
            val urlBuilder = "http://$targetIp:$targetPort".toHttpUrlOrNull()!!.newBuilder()

            // add every attribute
            for(attr in attributes)
            {
                urlBuilder.addQueryParameter(attr.attrName, attr.attrValue)
            }

            urlBuilder.port(targetPort.toInt())

            val url = urlBuilder.build().toString()

            val request: Request = Request.Builder().url(url).addHeader("Connection", "close").build()

            val call: Call = client.newCall(request)

            val response: Response = call.execute()

            return response.body?.string()
        }

        val nameFilter =
            InputFilter { source, start, end, dest, dstart, dend ->
                if (source != null && blockCharacterSet.contains("" + source)) {
                    ""
                } else null
            }
    }

    val selectModeFragment = SelectModeFragment()

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
        selectModeFragment.setViewPagerItem(selectedLedItem!!.currentMode.toInt())
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
                supportFragmentManager.beginTransaction().replace(R.id.mainFragmentContainer, selectModeFragment).commit()
                toolbar.title = selectedLedItem!!.customName
            }
            else -> {
                //show the modeSelectFragment if an LEDItem is clicked
                supportFragmentManager.beginTransaction().replace(R.id.mainFragmentContainer, selectModeFragment).commit()

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
