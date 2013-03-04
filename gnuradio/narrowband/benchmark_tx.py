#!/usr/bin/env python
#
# Copyright 2010,2011 Free Software Foundation, Inc.
#
# This file is part of GNU Radio
#
# GNU Radio is free software; you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation; either version 3, or (at your option)
# any later version.
#
# GNU Radio is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with GNU Radio; see the file COPYING.  If not, write to
# the Free Software Foundation, Inc., 51 Franklin Street,
# Boston, MA 02110-1301, USA.
#

from gnuradio import gr
from gnuradio import eng_notation
from gnuradio.eng_option import eng_option
from optparse import OptionParser
from gnuradio import filter
from gnuradio.filter import firdes
from gnuradio.gr import firdes
from gnuradio import blocks
from gnuradio import eng_notation

# From gr-digital
from gnuradio import digital

# from current dir
from transmit_path import transmit_path
from uhd_interface import uhd_transmitter

import time, struct, sys

#import os
#print os.getpid()
#raw_input('Attach and press enter')

class my_top_block(gr.top_block):
    def __init__(self, modulator, options):
        gr.top_block.__init__(self)

        self.txpath = [ ]
        use_sink = None
        if(options.tx_freq is not None):
            # Work-around to get the modulation's bits_per_symbol
            args = modulator.extract_kwargs_from_options(options)
            symbol_rate = options.bitrate / modulator(**args).bits_per_symbol()

            self.sink = uhd_transmitter(options.args, symbol_rate,
                                        options.samples_per_symbol,
                                        options.tx_freq, options.tx_gain,
                                        options.spec, options.antenna,
                                        options.verbose)
            sample_rate = self.sink.get_sample_rate()
            options.samples_per_symbol = self.sink._sps
            use_sink = self.sink
        elif(options.to_file is not None):
            sys.stderr.write(("Saving samples to '%s'.\n\n" % (options.to_file)))
            self.sink = gr.file_sink(gr.sizeof_gr_complex, options.to_file)
            self.throttle = gr.throttle(gr.sizeof_gr_complex*1, options.file_samp_rate)
            self.connect(self.throttle, self.sink)
            use_sink = self.throttle
        else:
            sys.stderr.write("No sink defined, dumping samples to null sink.\n\n")
            self.sink = gr.null_sink(gr.sizeof_gr_complex)

        # do this after for any adjustments to the options that may
        # occur in the sinks (specifically the UHD sink)
        self.txpath.append(transmit_path(modulator, options))
        self.txpath.append(transmit_path(modulator, options))

        samp_rate = 0
        if(options.tx_freq is not None):
            samp_rate = self.sink.get_sample_rate()
        else:
            samp_rate = options.file_samp_rate

        volume = options.split_amplitude
        band_transition = options.band_trans_width
        low_transition = options.low_trans_width
        guard_width = options.guard_width

        self.low_pass_filter_qv0 = gr.interp_fir_filter_ccf(2, firdes.low_pass(
            1, samp_rate, samp_rate/4-guard_width/2, low_transition, firdes.WIN_HAMMING, 6.76))
        self.freq_translate_qv0 = filter.freq_xlating_fir_filter_ccc(1, (options.num_taps, ), samp_rate/4, samp_rate)
        self.band_pass_filter_qv0 = gr.fir_filter_ccc(1, firdes.complex_band_pass(
            1, samp_rate, -samp_rate/2+guard_width, 0-guard_width, band_transition, firdes.WIN_HAMMING, 6.76))

        self.low_pass_filter_qv1 = gr.interp_fir_filter_ccf(2, firdes.low_pass(
            1, samp_rate, samp_rate/4-guard_width/2, low_transition, firdes.WIN_HAMMING, 6.76))
        self.freq_translate_qv1 = filter.freq_xlating_fir_filter_ccc(1, (options.num_taps, ), -samp_rate/4, samp_rate)
        self.band_pass_filter_qv1 = gr.fir_filter_ccc(1, firdes.complex_band_pass(
            1, samp_rate, 0+guard_width, samp_rate/2-guard_width, band_transition, firdes.WIN_HAMMING, 6.76))

        self.combiner = gr.add_vcc(1)
        self.volume_multiply = blocks.multiply_const_vcc((volume, ))

        self.connect((self.txpath[0], 0), (self.low_pass_filter_qv0, 0))
        self.connect((self.txpath[1], 0), (self.low_pass_filter_qv1, 0))

        self.connect((self.low_pass_filter_qv0, 0), (self.freq_translate_qv0, 0))
        self.connect((self.freq_translate_qv0, 0), (self.band_pass_filter_qv0, 0))

        self.connect((self.low_pass_filter_qv1, 0), (self.freq_translate_qv1, 0))
        self.connect((self.freq_translate_qv1, 0), (self.band_pass_filter_qv1, 0))

        self.connect((self.band_pass_filter_qv0, 0), (self.combiner, 0))
        self.connect((self.band_pass_filter_qv1, 0), (self.combiner, 1))

        self.connect((self.combiner, 0), (self.volume_multiply, 0))

        self.connect(self.volume_multiply, use_sink)

# /////////////////////////////////////////////////////////////////////////////
#                                   main
# /////////////////////////////////////////////////////////////////////////////

def main():

    def send_pkt(which, payload='', eof=False):
        return tb.txpath[which].send_pkt(payload, eof)

    mods = digital.modulation_utils.type_1_mods()

    parser = OptionParser(option_class=eng_option, conflict_handler="resolve")
    expert_grp = parser.add_option_group("Expert")

    parser.add_option("-m", "--modulation", type="choice", choices=mods.keys(),
                      default='qam',
                      help="Select modulation from: %s [default=%%default]"
                            % (', '.join(mods.keys()),))

    parser.add_option("-s", "--size", type="eng_float", default=1500,
                      help="set packet size [default=%default]")
    parser.add_option("-M", "--megabytes", type="eng_float", default=1.0,
                      help="set megabytes to transmit [default=%default]")
    parser.add_option("","--discontinuous", action="store_true", default=False,
                      help="enable discontinous transmission (bursts of 5 packets)")
    parser.add_option("","--from-file", default=None,
                      help="use intput file for packet contents")
    parser.add_option("","--to-file", default=None,
                      help="Output file for modulated samples")

    custom_grp = parser.add_option_group("Custom")
    custom_grp.add_option("","--band-trans-width", type="eng_float", default=75e3,
                      help="transition width for band pass filter")
    custom_grp.add_option("","--low-trans-width", type="eng_float", default=75e3,
                      help="transition width for low pass filter")
    custom_grp.add_option("","--guard-width", type="eng_float", default=100e3,
                      help="guard region width")
    custom_grp.add_option("","--file-samp-rate", type="eng_float", default=1e6,
                      help="file sample rate")
    custom_grp.add_option("","--split-amplitude", type="eng_float", default=0.15,
                      help="multiplier post split")
    custom_grp.add_option("","--rs-n", type="int", default=194,
                      help="reed solomon n")
    custom_grp.add_option("","--rs-k", type="int", default=188,
                      help="reed solomon k")
    custom_grp.add_option("","--num-taps", type="int", default=2,
                      help="reed solomon k")

    transmit_path.add_options(parser, expert_grp)
    uhd_transmitter.add_options(parser)

    for mod in mods.values():
        mod.add_options(expert_grp)

    (options, args) = parser.parse_args ()

    options.bitrate = 3000e3
    options.tx_gain = 25
    options.constellation_points = 16

    if len(args) != 0:
        parser.print_help()
        sys.exit(1)

    if options.from_file is not None:
        source_file = open(options.from_file, 'r')

    # build the graph
    tb = my_top_block(mods[options.modulation], options)

    r = gr.enable_realtime_scheduling()
    if r != gr.RT_OK:
        print "Warning: failed to enable realtime scheduling"

    tb.start()                       # start flow graph

    # generate and send packets
    nbytes = int(1e6 * options.megabytes)
    n = 0
    pktno = 0
    pkt_size = int(options.size)

    while n < nbytes:
        for i in range(2):
            if options.from_file is None:
                data = (pkt_size - 2) * chr(pktno & 0xff)
            else:
                data = source_file.read(pkt_size - 2)
                if data == '':
                    break;

            payload = struct.pack('!H', pktno & 0xffff) + data

            send_pkt(i, payload)

            n += len(payload)
            sys.stderr.write('.')
            if options.discontinuous and pktno % 5 == 4:
                time.sleep(1)
            pktno += 1

    send_pkt(0, eof=True)
    send_pkt(1, eof=True)

    tb.wait()                       # wait for it to finish

if __name__ == '__main__':
    try:
        main()
    except KeyboardInterrupt:
        pass
