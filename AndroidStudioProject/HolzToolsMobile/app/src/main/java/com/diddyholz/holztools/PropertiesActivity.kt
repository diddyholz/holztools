package com.diddyholz.holztools

import androidx.appcompat.app.AppCompatActivity
import android.os.Bundle
import android.view.Menu
import android.view.MenuItem
import androidx.fragment.app.Fragment
import kotlinx.android.synthetic.main.activity_main.*
import kotlinx.android.synthetic.main.activity_main.toolbar
import kotlinx.android.synthetic.main.activity_properties.*

class PropertiesActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_properties)

        setSupportActionBar(toolbar)

        toolbar.title = "Properties of " + intent.getStringExtra("LedItemName")
        toolbar.setNavigationIcon(R.drawable.ic_cancel)
        toolbar.setNavigationOnClickListener {
            finish()
        }

        supportFragmentManager.beginTransaction().add(R.id.preferenceFragmentContainer, PropertiesFragment()).commit()
    }

    override fun onCreateOptionsMenu(menu: Menu?): Boolean {
        menuInflater.inflate(R.menu.menu_properties_toolbar, menu)

        return true
    }



    override fun onOptionsItemSelected(item: MenuItem): Boolean {
        //apply

        return true
    }
}