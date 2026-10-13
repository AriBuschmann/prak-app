// Lädt die benötigten jQWidgets-Module. Reihenfolge ist wichtig!
import './jquery-global';
import 'jqwidgets-framework/jqwidgets/jqxcore';
import 'jqwidgets-framework/jqwidgets/jqxdata';
import 'jqwidgets-framework/jqwidgets/jqxbuttons';
import 'jqwidgets-framework/jqwidgets/jqxscrollbar';
import 'jqwidgets-framework/jqwidgets/jqxmenu';
import 'jqwidgets-framework/jqwidgets/jqxlistbox';
import 'jqwidgets-framework/jqwidgets/jqxdropdownlist';
import 'jqwidgets-framework/jqwidgets/jqxgrid';
import 'jqwidgets-framework/jqwidgets/jqxgrid.selection';
import 'jqwidgets-framework/jqwidgets/jqxgrid.sort';
import 'jqwidgets-framework/jqwidgets/jqxgrid.pager';
import 'jqwidgets-framework/jqwidgets/jqxgrid.columnsresize';
import 'jqwidgets-framework/jqwidgets/jqxwindow';
import 'jqwidgets-framework/jqwidgets/jqxcalendar';
import 'jqwidgets-framework/jqwidgets/jqxtooltip';
import 'jqwidgets-framework/jqwidgets/jqxdatetimeinput';

import 'jqwidgets-framework/jqwidgets/styles/jqx.base.css';
import 'jqwidgets-framework/jqwidgets/styles/jqx.light.css';

export { default as $ } from './jquery-global';

export const JQX_THEME = 'light';
